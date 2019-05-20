using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using MusicMachine.ThirdParty.Midi;

namespace MusicMachine.Music
{
//this is more of a tester.
public class MidiSongPlayer : Node
{
    private const byte DrumChannelNum = 0x09;
    private const int DrumBank = 128;
    private const int NumChannels = 16;
    private readonly Channel[] _channels = new Channel[NumChannels];
    private readonly MidiSong _song;
    private readonly MidiSongStepper _stepper = new MidiSongStepper();
    private AdsrPlayer[] _players;
    private Bank _bank;
    [Export] public string Bus = "master";
    [Export] public int MaxPolyphony = 128;
    [Export] public AudioStreamPlayer.MixTargetEnum MixTarget = AudioStreamPlayer.MixTargetEnum.Stereo;
    [Export] public float AmpDB = 20;
    [Export] public float VolumeDB = -10;
    [Export] public string SoundFontFile = "";
    private bool _ready;
    public MidiSongPlayer(MidiSong song)
    {
        _song = song;
        _stepper.OnEvent += OnEvent;
        for (var i = 0; i < _channels.Length; i++)
        {
            _channels[i] = new Channel();
            if (i == DrumChannelNum)
                _channels[i].Bank = DrumBank;
        }
    }

    public bool Playing { get; private set; }

    public override void _Ready()
    {
        SetProcess(false);
        CreatePlayers();
        PrepareBank();
        _ready = true;
    }
    private void CreatePlayers()
    {
        if (_players != null)
            return;
        _players = new AdsrPlayer[MaxPolyphony];
        for (var index = 0; index < _players.Length; index++)
        {
            var player = new AdsrPlayer();
            player.MixTarget = MixTarget;
            player.Bus = Bus;
            AddChild(player, true);
            _players[index] = player;
        }
    }
    private void PrepareBank()
    {
        if (_bank != null)
            return;
        var usedProgNums = new HashSet<int>();
        foreach (var x in _song.Tracks)
        foreach (var bankEvent in x.Events)
            switch (bankEvent)
            {
            case ProgramChangeEvent pce:
            {
                var bankProgNum = pce.ProgramNumber | (_channels[bankEvent.Channel].Bank << 7);
                usedProgNums.Add(bankProgNum);
                usedProgNums.Add(pce.ProgramNumber);
                break;
            }
            case BankSelectEvent bse:
                if (bankEvent.Channel != DrumChannelNum)
                    _channels[bankEvent.Channel].Bank = bse.Bank;
                break;
            }
        var usedProgNumArr = new Array<int>();
        foreach (var programNum in usedProgNums)
            usedProgNumArr.Add(programNum);

        _bank = new Bank(SoundFontFile, usedProgNumArr);
    }
    public void Play<TTImeSpan>(TTImeSpan atTime)
        where TTImeSpan : ITimeSpan
    {
        Play(TimeConverter.ConvertTo<MidiTimeSpan>(atTime, _song.TempoMap).TimeSpan);
    }
    public void Play(long atMidiTimeSpan = 0)
    {
        if (!_ready)
            throw new InvalidOperationException();
        StopAllNotes();
        Playing = true;
        _stepper.BeginPlay(_song, atMidiTimeSpan);
        SetProcess(true);
    }
    private void Stop()
    {
        _stepper.Stop();
        Playing = false;
        SetProcess(false);
    }
    public override void _Process(float delta)
    {
        if (_bank == null)
            return;
        if (!Playing)
            return;
        foreach (var channel in _channels)
            channel.ClearNotPlaying();
        if (!_stepper.Step(delta))
            Stop();
    }
    public void StopAllNotes()
    {
        foreach (var player in _players)
        {
            player.Stop();
        }
        foreach (var channel in _channels)
        {
            channel.NotesOn.Clear();
        }
    }
    private void OnEvent(long ignored, ChannelEvent @event)
    {
        //currently is verbatim. Will change.
        var channelNum = @event.Channel;
        var channel    = _channels[channelNum];
        switch (@event)
        {
        case NoteOnEvent noteOnEvent:
        {
            var keyNum = noteOnEvent.NoteNumber;
            //update??
            var preset     = _bank.GetPreset(channel.Program, channel.Bank);
            var instrument = preset[keyNum];
            if (instrument == null)
                return;
            if (channel.NotesOn.TryGetValue(keyNum, out var stopPlayer))
                stopPlayer.StartRelease();

            var player = GetIdlePlayer();
            if (player == null)
                return;
            player.Velocity = noteOnEvent.Velocity;
            player.UpdateChannelVolume(AmpDB, VolumeDB, channel);
            player.PitchBend = channel.PitchBend;
            player.SetInstrument(instrument);
            player.Play();
            if (channelNum != DrumChannelNum)
                channel.NotesOn[keyNum] = player;
            break;
        }
        case NoteOffEvent noteOffEvent:
        {
            var keyNum = noteOffEvent.NoteNumber;
            if (channel.NotesOn.TryGetAndRemove(keyNum, out var player))
                player?.StartRelease();
            break;
        }
        case VolumeChangeEvent volumeChangeEvent:
            channel.Volume = volumeChangeEvent.Volume / 127f;
            channel.UpdateVolume(AmpDB, VolumeDB);
            break;
        case ExpressionChangeEvent expressionChangeEvent:
        {
            channel.Expression = expressionChangeEvent.Expression / 127f;
            channel.UpdateVolume(AmpDB, VolumeDB);
            break;
        }
        case BankSelectEvent bankSelectEvent:
        {
            if (channelNum != DrumChannelNum)
                channel.Bank = bankSelectEvent.Bank;
            break;
        }
        case ProgramChangeEvent programChangeEvent:
        {
            channel.Program = programChangeEvent.ProgramNumber;
            break;
        }
        case PitchBendEvent pitchBendEvent:
        {
            channel.PitchBend = pitchBendEvent.PitchValue / 8192f - 1;
            channel.UpdatePitchBend();
            break;
        }
        default:
            Console.WriteLine($"Unprocessed Event: {@event}");
            break;
        }
    }
    private AdsrPlayer GetIdlePlayer()
    {
        var        minVol        = 100f;
        AdsrPlayer stoppedPlayer = null;
        var        oldestTime    = -1f;
        AdsrPlayer oldestPlayer  = null;
        foreach (var player in _players)
        {
            if (!player.Playing)
                return player;
            if (player.Releasing && player.CurrentVolume < minVol)
            {
                stoppedPlayer = player;
                minVol = player.CurrentVolume;
            }
            if (player.UsingTimer > oldestTime)
            {
                oldestPlayer = player;
                oldestTime = player.UsingTimer;
            }
        }
        return stoppedPlayer ?? oldestPlayer;
    }
}
}