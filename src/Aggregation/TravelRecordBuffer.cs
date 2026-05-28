using System;
using System.IO;
using System.Text;
using System.Threading;
using ODMatrix.Diagnostics;
using ODMatrix.Models;

namespace ODMatrix.Aggregation
{
    internal static class TravelRecordBuffer
    {
        private const int BufferSize = 10000;
        private const int MaxLinesPerFile = 100000;

        private static TravelRecord[] _bufferA;
        private static TravelRecord[] _bufferB;

        private static TravelRecord[] _currentBuffer;
        private static TravelRecord[] _flushBuffer;

        private static int _currentIndex;
        private static int _flushCount;

        private static readonly object _swapLock = new object();
        private static AutoResetEvent _flushEvent;
        private static Thread _ioThread;
        private static volatile bool _isRunning;
        private static volatile bool _isFlushing;

        private static string _csvFilePath;

        private static string _sessionStartTimeStr;
        private static float _lastGameTimeOfDay;   // 记录上一条记录的视觉时间
        private static int _visualDayCount;        // 记录游戏内的视觉天数
        private static int _currentLineCount;      // 用于追踪当前文件的行数
        private static int _fileSplitIndex;        // 文件分片后缀序号
        internal static int DroppedRecords { get; private set; }

        internal static void Initialize()
        {
            _bufferA = new TravelRecord[BufferSize];
            _bufferB = new TravelRecord[BufferSize];
            _currentBuffer = _bufferA;
            _flushBuffer = _bufferB;
            _currentIndex = 0;
            DroppedRecords = 0;

            _sessionStartTimeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _lastGameTimeOfDay = -1f;
            _visualDayCount = 1;
            _currentLineCount = 0;
            _fileSplitIndex = 0;

            _csvFilePath = null;

            _flushEvent = new AutoResetEvent(false);
            _isRunning = true;
            _isFlushing = false;

            _ioThread = new Thread(IoWorkerLoop)
            {
                IsBackground = true,
                Name = "ODMatrix_IOWorker"
            };
            _ioThread.Start();

            ModLogger.Info("TravelRecordBuffer initialized. Split rules: Visual Day bounds and " + MaxLinesPerFile + " lines/file.");
        }

        internal static void Enqueue(ref TravelRecord record)
        {
            lock (_swapLock)
            {
                if (_currentIndex < BufferSize)
                {
                    _currentBuffer[_currentIndex] = record;
                    _currentIndex++;
                }
                else
                {
                    if (_isFlushing)
                    {
                        DroppedRecords++;
                        return;
                    }

                    TravelRecord[] temp = _currentBuffer;
                    _currentBuffer = _flushBuffer;
                    _flushBuffer = temp;

                    _flushCount = _currentIndex;
                    _currentIndex = 0;
                    _isFlushing = true;


                    _currentBuffer[_currentIndex] = record;
                    _currentIndex++;

                    _flushEvent.Set();
                }
            }
        }

        internal static void Shutdown()
        {
            _isRunning = false;

            lock (_swapLock)
            {
                if (_currentIndex > 0)
                {
                    TravelRecord[] temp = _currentBuffer;
                    _currentBuffer = _flushBuffer;
                    _flushBuffer = temp;
                    _flushCount = _currentIndex;
                    _isFlushing = true;
                    _flushEvent.Set();
                }
            }

            if (_ioThread != null && _ioThread.IsAlive)
            {
                _ioThread.Join(2000);
            }

            if (_flushEvent != null)
            {
                _flushEvent.Close();
            }
        }

        private static void IoWorkerLoop()
        {
            while (_isRunning || _isFlushing)
            {
                _flushEvent.WaitOne();

                if (_flushCount > 0)
                {
                    try
                    {
                        WriteBufferToDisk();
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error("Failed to write CSV buffer to disk.", ex);
                    }
                    finally
                    {
                        _isFlushing = false;
                        _flushCount = 0;
                    }
                }
            }
        }

        private static void WriteBufferToDisk()
        {
            StringBuilder sb = new StringBuilder(_flushCount * 128);

            for (int i = 0; i < _flushCount; i++)
            {
                TravelRecord record = _flushBuffer[i];

                bool isNewVisualDay = false;
                if (_lastGameTimeOfDay >= 0f && record.GameTimeOfDay < (_lastGameTimeOfDay - 0.1f))
                {
                    isNewVisualDay = true;
                }
                _lastGameTimeOfDay = record.GameTimeOfDay;

                bool isLineLimitReached = _currentLineCount >= MaxLinesPerFile;

                if (_csvFilePath == null || isNewVisualDay || isLineLimitReached)
                {
                    if (sb.Length > 0 && _csvFilePath != null)
                    {
                        File.AppendAllText(_csvFilePath, sb.ToString());
                        sb.Length = 0;
                    }

                    if (isNewVisualDay)
                    {
                        _visualDayCount++;
                        _fileSplitIndex = 0;
                        ModLogger.Info("Transitioned to a new visual day. Current Visual Day: " + _visualDayCount);
                    }
                    else if (isLineLimitReached)
                    {
                        _fileSplitIndex++;
                        ModLogger.Info("Reached " + MaxLinesPerFile + " lines. Splitting file for Day " + _visualDayCount + ", Part " + _fileSplitIndex);
                    }

                    _currentLineCount = 0;

                    _csvFilePath = Path.Combine(
                        ModLogger.LogDirectoryPath,
                        "odmatrix_" + _sessionStartTimeStr + "_Day" + _visualDayCount + "_part" + _fileSplitIndex + ".csv"
                    );

                    if (!File.Exists(_csvFilePath))
                    {
                        File.WriteAllText(_csvFilePath, "RecordTime,GameTimeOfDay,CitizenId,Reason,TravelerType,OriginX,OriginZ,DestX,DestZ\n");
                    }
                }

                sb.Append(record.RecordTime).Append(',')
                  .Append(record.GameTimeOfDay.ToString("0.0000")).Append(',')
                  .Append(record.CitizenId).Append(',')
                  .Append(record.Reason).Append(',')
                  .Append((byte)record.TravelerType).Append(',')
                  .Append(record.OriginX).Append(',')
                  .Append(record.OriginZ).Append(',')
                  .Append(record.DestX).Append(',')
                  .Append(record.DestZ).Append('\n');

                _currentLineCount++;
            }

            if (sb.Length > 0 && _csvFilePath != null)
            {
                File.AppendAllText(_csvFilePath, sb.ToString());
            }
        }
    }
}