using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class SSTVCamera : MonoBehaviour {
	const int LINES = 120;
	const int HORIZONTAL_DATA_POINTS = 360;
	const float ASPECT_RATIO = 3f / 2f;
	const float SYNC_FREQ = 1200f;
	const float SYNC_PULSE = 0.005f;
	const float IMAGE_LINE_PULSE = 1f / 15f - SYNC_PULSE;
	const float MIN_DATA_FREQ = 1500f;
	const float MAX_DATA_FREQ = 2300f;
	const float BANDWIDTH = MAX_DATA_FREQ - MIN_DATA_FREQ;

	FreqMod freqMod => FreqMod.instance;

	Camera cam;
	RenderTexture rt;

	public static readonly (float frequency, float time)[] header = new (float, float)[9] {
		(1900, 0.3f),
		(1200, 0.01f),
		(1900, 0.3f),
		(1200, 0.03f),
		(1300, 0.03f),
		(1100, 0.03f),
		(1300, 0.15f),
		(1100, 0.03f),
		(1200, 0.03f),
	};
	

	void Awake() {
		cam = GetComponent<Camera>();
		rt = new RenderTexture(HORIZONTAL_DATA_POINTS, LINES, 0, RenderTextureFormat.ARGB32);
		cam.targetTexture = rt;
		cam.aspect = ASPECT_RATIO;
	}

	public void Capture() {
		cam.Render();

		Texture2D texture = new(rt.width, rt.height, TextureFormat.RFloat, false);


		RenderTexture.active = rt;
		texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
		texture.Apply();
		RenderTexture.active = null;

		// Flip the texture vertically.
		var pixels = texture.GetPixels();
		var flipped = new Color[pixels.Length];
		for (int i = 0; i < texture.height; i++) {
			for (int j = 0; j < texture.width; j++) {
				var col = pixels[(texture.height - i - 1) * texture.width + j];
				(col.r, col.g, col.b) = (Mathf.Clamp01(col.r), Mathf.Clamp01(col.g), Mathf.Clamp01(col.b));
				flipped[i * texture.width + j] = col;
			}
		}
		texture.SetPixels(flipped);

		var bytes = texture.GetRawTextureData();
		var data = new float[bytes.Length / sizeof(float)];
		System.Buffer.BlockCopy(bytes, 0, data, 0, bytes.Length);
		// Gamma correction.
		//data = data.Select(x => Mathf.Pow(x, 0.4545f)).ToArray();
		var pitch = texture.width;

		Transmit(data, pitch);
	}

	public void Transmit(float[] data, int pitch) {
		data = data ?? throw new System.ArgumentNullException("data");
		if (pitch <= 0) throw new System.ArgumentOutOfRangeException("pitch", "data pitch must be positive");

		FillBuffer(data, pitch);
	}

	public void FillBuffer(float[] data, int dataPitch) {
		if (data == null) return;
		freqMod.oscillator.Interrupt();

		int i;
		for (i = 0; i < header.Length; i++) {
			float freq;
			float time;
			(freq, time) = header[i];
			freqMod.oscillator.PlayFreq(freq, time);
		}
		
		int line;
		int column;
		for (line = 0, i = 0; line < data.Length / dataPitch; line++) {
			freqMod.oscillator.PlayFreq(SYNC_FREQ, SYNC_PULSE);

			for (column = 0; column < dataPitch; column++, i++) {
				var freq = IntensityToFrequency(data[i]);
				// If this is the last command, don't slide.
				if (i < data.Length - 1) {
					var nextFreq = IntensityToFrequency(data[i + 1]);
					freqMod.oscillator.PlaySlidingFreq(freq, nextFreq, IMAGE_LINE_PULSE / dataPitch);
				}
				else
					freqMod.oscillator.PlayFreq(freq, IMAGE_LINE_PULSE / dataPitch);
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float IntensityToFrequency(float value) =>
		Mathf.Clamp(MIN_DATA_FREQ + value * BANDWIDTH, MIN_DATA_FREQ, MAX_DATA_FREQ);
}