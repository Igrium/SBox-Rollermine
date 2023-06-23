﻿#nullable enable

using Sandbox;
using System;

namespace Rollermine;

public partial class Rollermine
{
    private Sound? CurrentSound;

    /// <summary>
    /// What DO you call a factor used to determine a factor?
    /// </summary>
    protected static readonly float SOUND_FACTOR_FACTOR = .005f;

    /// <summary>
    /// The epsilon to determine when the sound should be turned off.
    /// </summary>
    protected static readonly float SOUND_FACTOR_EPSILON = .2f;

    protected static readonly float SOUND_UPPER_LIMIT = 1.23f;

    [GameEvent.Tick.Client]
    protected virtual void TickSound()
    {
        float factor = GetSoundFactor();

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
            Log.Info(soundFactor);
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
        return Math.Min((LocalVelocity.Length + 1) * SOUND_FACTOR_FACTOR, SOUND_UPPER_LIMIT);
    }
}