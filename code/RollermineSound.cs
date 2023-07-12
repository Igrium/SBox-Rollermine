#nullable enable

using Sandbox;
using System;

namespace Rollermine;

public partial class Rollermine
{
    private Sound? CurrentSound;

    /// <summary>
    /// What DO you call a factor used to determine a factor?
    /// </summary>
    protected static readonly float SOUND_FACTOR_FACTOR = .06f;

    /// <summary>
    /// The epsilon to determine when the sound should be turned off.
    /// </summary>
    protected static readonly float SOUND_FACTOR_EPSILON = .1f;

    protected static readonly float SOUND_UPPER_LIMIT = 1.23f;

    [GameEvent.Tick.Server]
    protected virtual void TickSound()
    {
        float factor = GetSoundFactor();
        DebugOverlay.ScreenText($"vel: {factor}");

        if (factor < SOUND_FACTOR_EPSILON || IsStunned)
        {
            StopSound();
            return;
        }
        else
        {
            StartSound();
            float soundFactor = GetSoundFactor();
            //CurrentSound?.SetPitch(soundFactor);
            CurrentSound?.SetVolume(soundFactor);
        }
    }

    protected void StartSound()
    {
        if (CurrentSound.HasValue) return;
        CurrentSound = Sound.FromEntity("rmine_moveslow_loop1", this);
    }

    protected void StopSound()
    {
        if (!CurrentSound.HasValue) return;
        CurrentSound.Value.Stop();
        CurrentSound = null;
    }

    private float GetSoundFactor()
    {
        return Math.Min((AngularVelocity.AsVector3().Length + 1) * SOUND_FACTOR_FACTOR, SOUND_UPPER_LIMIT);
    }
}