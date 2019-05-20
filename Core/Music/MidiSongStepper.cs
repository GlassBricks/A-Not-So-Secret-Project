using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;

namespace MusicMachine.Music
{
public class MidiSongStepper
{
    private IEnumerator<IEnumerable<KeyValuePair<long, ChannelEvent>>> _stepper;

    public event Action<long, ChannelEvent> OnEvent;

    public event Action BeforeEvents;

    private long _curMicros;
    private bool _started;
    private TempoMap _tempoMap;

    public bool Playing => _stepper != null;

    public void BeginPlay(MidiSong midiSong, long start = 0)
    {
//        Stop();
        _stepper = new FlatteningMultipleIterator<KeyValuePair<long, ChannelEvent>>(
            midiSong.Tracks.Select(x => x.IterateTrackSingleLists(start, Stepper))).GetEnumerator();
        _tempoMap = midiSong.TempoMap;
        _started = false;
        _curMicros = start;
    }
    public void Stop()
    {
        _stepper = null;
        _tempoMap = null;
        _started = false;
    }
    private long Stepper(long ignored)
    {
        return TimeConverter.ConvertTo<MidiTimeSpan>(new MetricTimeSpan(_curMicros), _tempoMap);
    }
    public bool Step(float seconds)
    {
        return StepMicros(seconds.SecondsToMicros());
    }
    public bool StepMicros(long micros)
    {
        if (micros < 0)
            throw new InvalidOperationException();
        if (_stepper == null)
            return false;
        if (!_started)
            _started = true;
        else
            _curMicros += micros;

        if (!_stepper.MoveNext())
        { //ended
            Stop();
            return false;
        }
        Debug.Assert(_stepper.Current != null, "stepper.Current != null");
        if (OnEvent != null)
            foreach (var pair in _stepper.Current)
                OnEvent?.Invoke(pair.Key, pair.Value);
        return true;
    }
}
}