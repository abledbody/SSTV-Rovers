using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class FreqMod : MonoBehaviour {
	public static FreqMod instance;

	public OscillatorState oscillator = new(false);
	int bufferPosition = 0;
	float sampleFraction;


	void Awake() {
		instance = this;
		sampleFraction = 1f / AudioSettings.outputSampleRate;
	}

	public void OnAudioFilterRead(float[] data, int channels) {
		bufferPosition = 0;

		while (bufferPosition < data.Length) {
			var samplesGenerated = oscillator.Generate(data, bufferPosition, sampleFraction, channels);
			
			bufferPosition += samplesGenerated;
			// If no samples were generated, leave the rest of the buffer empty.
			if (samplesGenerated == 0) break;
		}
	}
}

/// <summary>Represents the state of an oscillator.</summary>
public struct OscillatorState {
	const float CLIP_THRESHOLD = 0.7f;
	
	/// <summary>The current phase of the oscillator.</summary>
	float phase;
	/// <summary>The current frequency of the oscillator.</summary>
	float frequency;
	/// <summary>Whether the oscillator is currently generating.</summary>
	bool generating;
	/// <summary>The target frequency of an oscillator slide.</summary>
	float frequencySlideRate;
	/// <summary>The time remaining until the oscillator asks for more data.</summary>
	float waitTime;

	private readonly ConcurrentQueue<Command> commandBuffer;

	public OscillatorState(bool _) {
		phase = 0;
		frequency = 0;
		generating = false;
		frequencySlideRate = 0;
		waitTime = 0;
		commandBuffer = new();
	}

	/// <summary>Generates a sine wave into the buffer.</summary>
	/// <param name="buffer">The buffer to write to.</param>
	/// <param name="offset">The offset into the buffer to start writing at.</param>
	/// <param name="sampleRate">The sample rate of the audio system.</param>
	/// <param name="channels">The number of channels in the audio system.</param>
	/// <returns>The number of samples generated.</returns>
	public int Generate(float[] buffer, int offset, float sampleFraction, int channels) {
		if (buffer == null) throw new ArgumentNullException("buffer");
		
		int i = offset;
		for (; i < buffer.Length; i += channels) {
			if (waitTime <= 0) {
				generating = false;
				ExecuteCommand();
			}

			phase = (phase + frequency * sampleFraction) % 1;
			
			frequency += frequencySlideRate * sampleFraction;
			
			if (waitTime > 0) waitTime -= sampleFraction;

			var value = generating ? Mathf.Sin(phase * 2 * Mathf.PI) : 0;
			for (int j = 0; j < channels; j++)
				buffer[i + j] = value;
		}

		return i - offset;
	}

	public void ExecuteCommand() {
		if (commandBuffer.TryDequeue(out var command)) {
			if (command.slide) {
				frequency = command.frequency;
				frequencySlideRate = (command.endFrequency - command.frequency) / command.time;
				waitTime += command.time;
				generating = true;
			}
			else {
				frequency = command.frequency;
				frequencySlideRate = 0;
				waitTime += command.time;
				generating = true;
			}
		}
	}

	/// <summary>Plays a frequency for a given time.</summary>
	/// <param name="frequency">The frequency to play.</param>
	/// <param name="time">The time to play for.</param>
	public void PlayFreq(float frequency, float time) =>
		commandBuffer.Enqueue(new(frequency, time));

	/// <summary>Plays a sliding frequency for a given time.</summary>
	/// <param name="startFrequency">The frequency to start at.</param>
	/// <param name="endFrequency">The frequency to end at.</param>
	/// <param name="time">The time to slide for.</param>
	public void PlaySlidingFreq(float startFrequency, float endFrequency, float time) =>
		commandBuffer.Enqueue(new(startFrequency, endFrequency, time));
	
	public void Interrupt() {
		commandBuffer.Clear();
		generating = false;
	}

	struct Command {
		public float frequency;
		public float endFrequency;
		public float time;
		public bool slide;

		public Command(float frequency, float time) {
			this.frequency = frequency;
			endFrequency = 0;
			this.time = time;
			slide = false;
		}

		public Command(float frequency, float endFrequency, float time) {
			this.frequency = frequency;
			this.endFrequency = endFrequency;
			this.time = time;
			slide = true;
		}
	}
}