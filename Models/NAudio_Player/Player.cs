using ShareInstances;
using ShareInstances.Instances;
using ShareInstances.Services.Entities;

using NAudio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NAudioPlayerCore.Models;
public class NAudioPlayer : IPlayer
{
    #region Player Indentity Properties
    public Guid PlayerId => Guid.NewGuid();
    public string PlayerName => "NAudio Player";
    #endregion

    #region Player Resource Properties
    public CurrentEntity CurrentEntity {get; private set;}
    public Track CurrentTrack { get; private set; }

    public int PlaylistPoint {get; private set;}
    public IList<Track> Collection {get; set;}

    
    #endregion

    #region Playbacker Properties
    public static NAudioPlaybacker _audioPlayer = new();
    private float volume;
    public TimeSpan TotalTime => _audioPlayer.TotalTime;

    public TimeSpan CurrentTime
    {
        get => _audioPlayer.CurrentTime; 
        set => _audioPlayer.CurrentTime = value; 
    }
    #endregion

    #region Player State Properties
    public bool IsEmpty { get; private set; } = true;
    public bool IsSwipe { get; private set; } = false;
    public bool PlayerState { get; private set; } = false;
    #endregion

    #region Volume Presenters
    public float MaxVolume {get; private set;} = 1;
    public float MinVolume {get; private set;} = 0;
    public float CurrentVolume 
    {
        get => _audioPlayer.Volume;
        set => _audioPlayer.OnVolumeChanged(value);
    }
    #endregion

    #region Actions
    private static Action notifyAction;
    #endregion

    #region Events
    private event Action ShuffleCollection;
    public event Action TrackStarted;
    #endregion

    #region ctor
    public NAudioPlayer()
    {}

    public void DropTrack(Track track)
    {
        CleanUpPlayer();

        CurrentTrack = track;
        InitAudioPlayer();
        IsEmpty = false;
    }

    public void DropPlaylist(Playlist playlist, int index=0)
    {
        CleanUpPlayer();

        Collection = playlist.GetTracks();
        PlaylistPoint = index;
        CurrentTrack = Collection[PlaylistPoint];
        InitAudioPlayer(PlaylistPoint);

        IsEmpty = false;
        IsSwipe = true;
    }

    #endregion

    #region PlayerInitialization
    public void InitAudioPlayer()
    {
        PlayerState = true;
        notifyAction?.Invoke();
        _audioPlayer.SetInstance(CurrentTrack);

        _audioPlayer.TrackFinished += () => 
        {
            _audioPlayer.Stop();
            PlayerState = false;
            notifyAction?.Invoke();
        };
    }

    public void InitAudioPlayer(int index)
    {
        if(Collection.Count > 0)
        {
            notifyAction?.Invoke(); 
            _audioPlayer.SetInstance(CurrentTrack);

            AutoDrop();
        }
    }

    public void SetNotifier(Action callBack)
    {
        notifyAction = callBack;
    }
    #endregion

    #region Player_Buttons
    public async Task Play()
    {
        await Task.Run(() => _audioPlayer.Play());  
        PlayerState = true;
        notifyAction?.Invoke(); 
    }

    public async Task StopPlayer()
    {
        await Task.Run(() => _audioPlayer.Stop());

        PlayerState = false;    
        notifyAction?.Invoke(); 
    }
    
    public async Task Pause_ResumePlayer()
    {
        TrackStarted?.Invoke();
        switch (_audioPlayer.PlaybackState)
        {
            case NAudio.Wave.PlaybackState.Stopped:
                PlayerState = true; 
                notifyAction?.Invoke();
                await Task.Run(() => _audioPlayer.Play());
                break;
            case NAudio.Wave.PlaybackState.Paused:
                PlayerState = true;
                notifyAction?.Invoke();   
                await Task.Run(() => _audioPlayer.Pause());
                break;
            case NAudio.Wave.PlaybackState.Playing:
                PlayerState = false;
                notifyAction?.Invoke();    
                await Task.Run(() => _audioPlayer.Pause());
                break;
        }

    }

    public async Task ShuffleTrackCollection()
    {
        await Task.Run(() => ShuffleCollection?.Invoke());
    }
    
    public async Task ChangeVolume(float volume)
    {
        _audioPlayer.OnVolumeChanged(volume);
    }

    public async Task RepeatTrack()
    {
        await Task.Run(() => _audioPlayer.Repeat());
    }

    #endregion

    #region Shuffle Methods
    private void OnShuffleCollection()
    {
        Shuffle();
        InitAudioPlayer(index: 0);
    }

    private void Shuffle()
    {
        var tmp = Collection.OrderBy(t => Guid.NewGuid())
                            .ToList();
        Collection = tmp;
    }
    #endregion

    #region Drop Methods
    private void AutoDrop() 
    {
        _audioPlayer.TrackFinished += DropNext;
    }
    
    public async void DropNext() 
    {
        await Task.Run(() => {
            if ((IsSwipe) && (!IsEmpty))
                DropMediaInstance(true);
        });
    }

    public async void DropPrevious() 
    {
        await Task.Run(() => {
            if ((IsSwipe) && (!IsEmpty))
                DropMediaInstance(false);
        });
    }

    private void DropMediaInstance(bool direct)
    {
        _audioPlayer.TrackFinished -= DropNext;

        _audioPlayer.Stop();
        PlayerState = false;
        notifyAction?.Invoke();
        
        DragPointer(direct);   
        SetMedia();
        notifyAction?.Invoke();
        Pause_ResumePlayer();
    }
    #endregion

    #region Private Methods
    private void SetMedia()
    {
        CurrentTrack = Collection[PlaylistPoint];
        _audioPlayer.SetInstance(CurrentTrack);
    }

    private void DragPointer(bool direct)
    {
        if(direct)
        {
            if (PlaylistPoint == Collection.Count - 1)
            {
                PlaylistPoint = 0;
            }
            else
            {
                PlaylistPoint++; 
            }
        }
        else
        {
            if (PlaylistPoint == 0)
            {
                PlaylistPoint = Collection.Count - 1;
            }   
            else
            {
                PlaylistPoint--; 
            }
        }
    }

    private void CleanUpPlayer()
    {
        _audioPlayer.TrackFinished -= DropNext;

        PlayerState = false;
        notifyAction?.Invoke();
        _audioPlayer.Stop();
        PlaylistPoint = 0;
        CurrentEntity = default!;

        if(Collection.Count > 0)
        {
            Collection.Clear();
        }
    }
    #endregion
}