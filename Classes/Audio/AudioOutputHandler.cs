using Silk.NET.OpenAL;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Gamelist_Manager.Classes.Audio;

public class AudioOutputHandler : IDisposable
{
    private readonly AL _al;
    private readonly ALContext _alc;
    private unsafe Device* _device;
    private unsafe Context* _context;

    private uint _source;
    private readonly uint[] _buffers;
    private const int BufferCount = 4;

    private readonly ConcurrentQueue<short[]> _queue = new();
    private readonly ConcurrentQueue<uint> _freeBuffers = new();
    private CancellationTokenSource _cts = new();
    private Task _playbackTask;

    public unsafe AudioOutputHandler(int sampleRate = 44100)
    {
        _al = AL.GetApi();
        _alc = ALContext.GetApi();

        _device = _alc.OpenDevice(string.Empty);
        if (_device == null)
            throw new Exception("Failed to open audio device.");

        _context = _alc.CreateContext(_device, null);
        _alc.MakeContextCurrent(_context);

        _source = _al.GenSource();
        _buffers = new uint[BufferCount];
        for (int i = 0; i < BufferCount; i++)
        {
            _buffers[i] = _al.GenBuffer();
            _freeBuffers.Enqueue(_buffers[i]);
        }

        _playbackTask = Task.Run(PlaybackLoop);
    }

    public void Enqueue(short[] samples)
    {
        _queue.Enqueue(samples);
    }

    private unsafe void PlaybackLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            _al.GetSourceProperty(_source, GetSourceInteger.BuffersProcessed, out int processed);

            while (processed > 0)
            {
                uint buffer = 0;
                _al.SourceUnqueueBuffers(_source, 1, &buffer);
                processed--;
                _freeBuffers.Enqueue(buffer);
            }

            while (_freeBuffers.TryDequeue(out uint buffer))
            {
                if (_queue.TryDequeue(out short[]? samples))
                {
                    FillAndQueueBuffer(buffer, samples);
                }
                else
                {
                    _freeBuffers.Enqueue(buffer);
                    break;
                }
            }

            _al.GetSourceProperty(_source, GetSourceInteger.SourceState, out int state);
            _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
            if ((SourceState)state != SourceState.Playing && queued > 0)
            {
                _al.SourcePlay(_source);
            }

            Thread.Sleep(5);
        }
    }

    private unsafe void FillAndQueueBuffer(uint buffer, short[] samples)
    {
        fixed (short* pSamples = samples)
        {
            _al.BufferData(buffer, BufferFormat.Stereo16, pSamples, samples.Length * sizeof(short), 44100);
        }
        _al.SourceQueueBuffers(_source, 1, &buffer);
    }

    public unsafe void Dispose()
    {
        _cts.Cancel();
        
        try { _playbackTask.Wait(500); } catch { }
        
        _al.SourceStop(_source);
        
        uint[] queuedBuffers = new uint[BufferCount];
        _al.GetSourceProperty(_source, GetSourceInteger.BuffersQueued, out int queued);
        while (queued > 0)
        {
            uint buffer = 0;
            _al.SourceUnqueueBuffers(_source, 1, &buffer);
            queued--;
        }

        _al.DeleteSource(_source);
        _al.DeleteBuffers(_buffers);

        _alc.MakeContextCurrent(null);
        _alc.DestroyContext(_context);
        _alc.CloseDevice(_device);

        _al.Dispose();
        _alc.Dispose();
    }
}
