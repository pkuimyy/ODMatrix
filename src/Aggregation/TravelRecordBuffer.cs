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
        internal static int DroppedRecords { get; private set; }

        internal static void Initialize()
        {
            _bufferA = new TravelRecord[BufferSize];
            _bufferB = new TravelRecord[BufferSize];
            _currentBuffer = _bufferA;
            _flushBuffer = _bufferB;
            _currentIndex = 0;
            DroppedRecords = 0;

            _flushEvent = new AutoResetEvent(false);
            _isRunning = true;
            _isFlushing = false;

            _csvFilePath = Path.Combine(ModLogger.LogDirectoryPath, "odmatrix_records_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
            File.WriteAllText(_csvFilePath, "RecordTime,CitizenId,Reason,TravelerType,OriginX,OriginZ,DestX,DestZ\n");

            _ioThread = new Thread(IoWorkerLoop)
            {
                IsBackground = true,
                Name = "ODMatrix_IOWorker"
            };
            _ioThread.Start();

            ModLogger.Info("TravelRecordBuffer initialized with Double Buffering. Buffer size: " + BufferSize);
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
            StringBuilder sb = new StringBuilder(_flushCount * 64);

            for (int i = 0; i < _flushCount; i++)
            {
                TravelRecord record = _flushBuffer[i];

                sb.Append(record.RecordTime).Append(',')
                  .Append(record.CitizenId).Append(',')
                  .Append(record.Reason).Append(',')
                  .Append((byte)record.TravelerType).Append(',')
                  .Append(record.OriginX).Append(',')
                  .Append(record.OriginZ).Append(',')
                  .Append(record.DestX).Append(',')
                  .Append(record.DestZ).Append('\n');
            }

            File.AppendAllText(_csvFilePath, sb.ToString());
        }
    }
}