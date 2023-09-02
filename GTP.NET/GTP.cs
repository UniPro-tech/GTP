using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UE = UnityEngine;

namespace GTP.NET
{
    /// <summary>
    /// GTPと通信するための.NET Standert2.0ライブラリ。
    /// Unity用。
    /// </summary>
    public class GTP
    {
        /// <summary>
        /// パス
        /// </summary>
        public string GTP_path;
        /// <summary>
        /// GTPのプロセスの実体
        /// </summary>
        private Process GTPprocess;

        // -- 読み取り専用の技術的な限界の変数 --
        const int KIFU_MAX = 2048;
        enum MoveResult
        {
            MOVE_SUCCESS,    // 成功
            MOVE_SUICIDE,    // 自殺手
            MOVE_KOU,        // コウ
            MOVE_EXIST,      // 既に石が存在
            MOVE_FATAL,      // それ以外

            // 以下はset_kifu_time()で利用
            MOVE_PASS_PASS,  // 連続パス、対局終了
            MOVE_KIFU_OVER,  // 手数が長すぎ
            MOVE_RESIGN,     // 投了
        }

        // -- 囲碁で使う変数 --
        int[] total_time = new int[2];
        int tesuu = 0;
        int all_tesuu = 0;
        List<int[]> kifu = new List<int[]>(KIFU_MAX);

        public GTP(string path)
        {
            GTP_path = path;
        }

        /// <summary>
        /// GTPエンジンファイルの存在チェック。
        /// </summary>
        /// <param name="path">
        /// [GTP].exeのパス。
        /// </param>
        /// <returns>
        /// true or false
        /// </returns>
        private bool IsGtpEngineExist()
        {
            if (File.Exists(GTP_path))
            {
                UE.Debug.Log("GTP is found.");
                return true;
            }
            else
            {
                UE.Debug.Log("GTP is NOT found.");
                return false;
            }
        }

        /// <summary>
        /// GTPエンジンを起動する。
        /// </summary>
        public void Start()
        {
            bool IsExist = IsGtpEngineExist();

            //見つからなかった際にエラー投げる。
            if (!IsExist)
            {
                throw new Exception("Engine not found.");
            }

            GTPprocess = new Process();
            GTPprocess.StartInfo.FileName = GTP_path;
            GTPprocess.StartInfo.UseShellExecute = false;
            GTPprocess.StartInfo.RedirectStandardInput = true;
            GTPprocess.StartInfo.RedirectStandardOutput = true;

            GTPprocess.Start();
            UE.Debug.Log("GTP is ready.");
        }

        /// <summary>
        /// コマンドを送信するメソッド。
        /// </summary>
        /// <param name="command">コマンド名。これをそのままGTPに送る。</param>
        /// <returns>アウトプット。</returns>
        public string SendCommand(string command)
        {
            using (var inputWriter = GTPprocess.StandardInput)
            using (var outputReader = GTPprocess.StandardOutput)
            {
                inputWriter.WriteLine(command);
                inputWriter.WriteLine("quit");

                string output = outputReader.ReadToEnd();
                return output;
            }
        }

        /// <summary>
        /// 経過時間を示す。
        /// </summary>
        /// <param name="startTicks">開始した時間を示す。long startTicks = Stopwatch.GetTimestamp();など</param>
        /// <returns></returns>
        public static double GetSpendTime(long startTicks)
        {
            return (double)(Stopwatch.GetTimestamp() - startTicks) / Stopwatch.Frequency;
        }

        /// <summary>
        ///  累計の思考時間を計算しなおすだけ。現在の手番の累計思考時間を返す
        /// </summary>
        /// <returns></returns>
        public int CountTotalTime()
        {
            int i, n;

            total_time[0] = 0; // 先手の合計の思考時間。
            total_time[1] = 0; // 後手
            for (i = 1; i <= tesuu; i++)
            {
                if ((i & 1) == 1)
                    n = 0; // 先手
                else
                    n = 1;
                total_time[n] += kifu[i][2];
            }
            return total_time[tesuu & 1]; // 思考時間。先手[0]、後手[1]の累計思考時間。
        }

        /// <summary>
        /// GTPを終了
        /// </summary>
        public void Close()
        {
            GTPprocess.WaitForExit();
            GTPprocess.Close();
        }
    }
}
