using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using KModkit;

using RNG = UnityEngine.Random;

public class Dreamcipher : MonoBehaviour
{
	// Standardized logging
	private static int globalLogID = 0;
	private int thisLogID;
	private bool moduleSolved;
	private bool moduleActive = false;

	public KMBombInfo bombInfo;
	public KMAudio bombAudio;
	public KMBombModule bombModule;

	private string targetWord;
	private int[] glyphList;

	private bool inInputMode = false;
	private int inputOnChar = 0;

	// ------------------
	// Animation Nonsense
	// ------------------

	public SpriteRenderer[] glyphDisplays;
	public TextMesh[] charDisplays;
	public Sprite[] glyphs;

	private bool glyphDisplayFullyOn = false;
	private float glyphDisplayAngle = 70f;
	private float staticDisplayTimer = -1f;
	private float inputDisplayAngle = 290f;

	// Returns color object, modifies transform to place in proper location
	Color SetUpAngledDisplay(float angle, Color baseColor, Transform dPosAng)
	{
		if (angle > 270f)
			baseColor.a *= (290f-angle)/20f;
		else if (angle < 90f)
			baseColor.a *= (angle-70f)/20f;

		float radians = angle * Mathf.Deg2Rad;
		dPosAng.localPosition = new Vector3(Mathf.Sin(radians)*0.06125f, Mathf.Cos(radians)*0.06125f, 0f);
		dPosAng.localRotation = Quaternion.Euler(0f, 0f, -angle-180f);
		return baseColor;
	}

	IEnumerator DisplayGlyphs(bool firstTime)
	{
		int cyPos, glPos;
		float curAngle;
		Color dispColor = new Color(1f, 1f, 1f, 1f);

		inInputMode = false;
		staticDisplayTimer = -1f;

		// Fade in controller (skipped the first time around)
		if (!firstTime)
			dispColor.a = 0.05f;

		// Main loop
		while (!inInputMode)
		{
			if (staticDisplayTimer < 0)
				glyphDisplayAngle += Time.deltaTime*12f; // Framerate independent
			else
				staticDisplayTimer -= Time.deltaTime;

			curAngle = glyphDisplayAngle;
			for (cyPos = 0; curAngle > 290f; curAngle -= 45f, ++cyPos);
			for (glPos = 0; curAngle > 70f; curAngle -= 45f, ++cyPos, ++glPos)
			{
				glyphDisplays[glPos].gameObject.SetActive(true);
				glyphDisplays[glPos].sprite = glyphs[glyphList[cyPos & 0xF]];
				glyphDisplays[glPos].color = SetUpAngledDisplay(curAngle, dispColor, glyphDisplays[glPos].transform);
			}
			for (; glPos < glyphDisplays.Length; ++glPos)
				glyphDisplays[glPos].gameObject.SetActive(false);
			if (dispColor.a < 0.99f && (dispColor.a += 0.05f) >= 0.99f)
				dispColor.a = 1f;

			if (glyphDisplayAngle >= 966f)
			{
				glyphDisplayAngle -= 720f;
				glyphDisplayFullyOn = true;
			}
			else if (glyphDisplayFullyOn && glyphDisplayAngle <= 250f)
				glyphDisplayAngle += 720f;

			if (glyphDisplayAngle <= 70f)
				glyphDisplayAngle = 70f;
			yield return null;
		}

		// Smooth fadeaway when input mode is engaged
		float[] origAlpha = new float[glyphDisplays.Length];
		for (glPos = 0; glPos < glyphDisplays.Length; ++glPos)
			origAlpha[glPos] = glyphDisplays[glPos].color.a;

		for (float alpha = 0.95f; alpha > 0f; alpha -= 0.05f)
		{
			// Rapid button mashing?
			if (!inInputMode)
				yield break;

			for (glPos = 0; glPos < glyphDisplays.Length; ++glPos)
			{
				dispColor.a = origAlpha[glPos] * alpha;
				glyphDisplays[glPos].color = dispColor;
			}
			yield return null;
		}
		for (glPos = 0; glPos < glyphDisplays.Length; ++glPos)
			glyphDisplays[glPos].gameObject.SetActive(false);
		yield break;
	}

	IEnumerator DisplayInput()
	{
		float timeBoundary;
		float curAngle, angleTarget, distToTarget;
		string display = "NOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLM";
		int cyPos, txPos;
		Color dispColor = new Color(1f, 1f, 1f, 1f);
		Color hiliteColor = new Color(1f, 1f, 0f, 1f);

		dispColor.a = 0.05f;
		inInputMode = true;
		inputDisplayAngle = 290f;

		while (inInputMode)
		{
			// Adjust to the center of the highlighted character.
			angleTarget = 492f + (inputOnChar * 24f);
			distToTarget = angleTarget - inputDisplayAngle;
			if (Mathf.Abs(distToTarget) > 336f)
			{
				inputDisplayAngle += (inputDisplayAngle > 804f) ? -624f : 624f; // Warp to the other side...
				distToTarget = angleTarget - inputDisplayAngle; // Then recalculate.
			}
			timeBoundary = (Time.deltaTime*240f);
			inputDisplayAngle += Mathf.Clamp(distToTarget/8, -timeBoundary, timeBoundary);

			try
			{
				curAngle = inputDisplayAngle;
				for (cyPos = 0; curAngle > 290f; curAngle -= 24f, ++cyPos);
				for (txPos = 0; curAngle > 70f; curAngle -= 24f, ++cyPos, ++txPos)
				{
					charDisplays[txPos].gameObject.SetActive(true);
					charDisplays[txPos].text = ""+display[cyPos];
					charDisplays[txPos].color = SetUpAngledDisplay(curAngle, ((cyPos+13)%26 == inputOnChar) ? hiliteColor : dispColor, charDisplays[txPos].transform);
				}
				for (; txPos < charDisplays.Length; ++txPos)
					charDisplays[txPos].gameObject.SetActive(false);
			}
			catch (IndexOutOfRangeException)
			{
				// Aha! Screw your lagspikes. Go back to a sane value.
				inputDisplayAngle = 492f;
			}

			if (dispColor.a < 0.99f && (dispColor.a += 0.05f) > 0.99f)
				dispColor.a = 1f;
			yield return null;
		}

		// Smooth fadeaway when input mode is disengaged
		float[] origAlpha = new float[charDisplays.Length];
		for (txPos = 0; txPos < charDisplays.Length; ++txPos)
			origAlpha[txPos] = charDisplays[txPos].color.a;

		for (float alpha = 0.95f; alpha > 0f; alpha -= 0.05f)
		{
			// Rapid button mashing?
			if (inInputMode)
				yield break;

			for (txPos = 0; txPos < charDisplays.Length; ++txPos)
			{
				dispColor.a = origAlpha[txPos] * alpha;
				charDisplays[txPos].color = dispColor;
			}
			yield return null;
		}
		for (txPos = 0; txPos < charDisplays.Length; ++txPos)
			charDisplays[txPos].gameObject.SetActive(false);
		yield break;
	}


	// ----------------------
	// Resizing input textbox
	// ----------------------

	// We don't just read the character info from the font, because with a curved display, 
	// letters with little detail at the bottom look like they have pretty bad kerning problems.
	private static readonly float[] __characterWidths = new float[26] {
		5.04f, 4.44f, 4.56f, 4.56f, 4.08f, 4.08f, 4.68f, // ABCDEFG
		4.56f, 1.92f, 4.32f, 4.44f, 3.84f, 5.16f, 4.68f, // HIJKLMN
		4.68f, 4.44f, 4.56f, 4.56f, 4.56f, 3.72f, 4.68f, // OPQRSTU
		4.08f, 5.16f, 5.04f, 4.08f, 4.68f                // VWXYZ
	};

	public TextMesh[] wordDisplays;
	private string wordInput = "";
	private Coroutine wordClearCoroutine;

	void SetWordAngledPosition(float angle, Transform dPosAng)
	{
		float radians = angle * Mathf.Deg2Rad;
		dPosAng.localPosition = new Vector3(Mathf.Sin(radians)*0.06125f, Mathf.Cos(radians)*0.06125f, 0f);
		dPosAng.localRotation = Quaternion.Euler(0f, 0f, -angle);
	}

	void UpdateWordDisplay()
	{
		int i = 0;
		float curAngle = 0f;

		for (; i < wordInput.Length; ++i)
		{
			wordDisplays[i].gameObject.SetActive(true);
			wordDisplays[i].color = new Color(1f, 1f, 1f, 1f);
			wordDisplays[i].text = ""+wordInput[i];
			curAngle -= __characterWidths[wordInput[i] - 'A'];
		}
		for (; i < wordDisplays.Length; ++i)
			wordDisplays[i].gameObject.SetActive(false);

		for (i = 0; i < wordInput.Length; ++i)
		{
			curAngle += __characterWidths[wordInput[i] - 'A'];
			SetWordAngledPosition(curAngle, wordDisplays[i].transform);
			curAngle += __characterWidths[wordInput[i] - 'A'];
		}
	}

	IEnumerator WordDisplayShutdown()
	{
		Color[] dispColors = new Color[wordDisplays.Length];
		float[] allAngles = new float[wordDisplays.Length];
		float curAngle = 0f;
		int i, numDisplays = 0;

		while (numDisplays < wordDisplays.Length)
		{
			if (!wordDisplays[numDisplays].gameObject.activeSelf)
				break; // Don't bother iterating further.
			dispColors[numDisplays] = wordDisplays[numDisplays].color;
			curAngle -= __characterWidths[wordInput[numDisplays++] - 'A'];
		}
		for (i = 0; i < numDisplays; ++i)
		{
			curAngle += __characterWidths[wordInput[i] - 'A'];
			allAngles[i] = curAngle;
			curAngle += __characterWidths[wordInput[i] - 'A'];
		}	

		char[] _randomRemap = "ABCDEFGHIJKLMNOPQRSTUVWXYZ!@#$%&1234567890".ToCharArray();
		for (float fade = 1f; fade > 0f; fade -= 0.006f)
		{
			for (i = 0; i < numDisplays; ++i)
			{
				dispColors[i].a = fade;
				wordDisplays[i].color = dispColors[i];

				// Space letters apart while moving
				allAngles[i] *= 1.006f;
				SetWordAngledPosition(allAngles[i], wordDisplays[i].transform);

				// Randomly "glitch" out characters as "power" fades.
				if (RNG.Range(-3.4f, 0.6f) > fade)
					wordDisplays[i].text = ""+(_randomRemap[RNG.Range(0, _randomRemap.Length)]);
			}
			yield return null;
		}

		for (i = 0; i < numDisplays; ++i)
			wordDisplays[i].gameObject.SetActive(false);
		yield break;
	}

	IEnumerator FadeOutAndClearWordDisplay()
	{
		Color[] dispColors = new Color[wordDisplays.Length];
		int i, numDisplays = 0;

		while (numDisplays < wordDisplays.Length)
		{
			if (!wordDisplays[numDisplays].gameObject.activeSelf)
				break; // Don't bother iterating further.
			dispColors[numDisplays] = wordDisplays[numDisplays].color;
			++numDisplays;
		}

		for (float fade = 1f; fade > 0f; fade -= 0.02f)
		{
			for (i = 0; i < numDisplays; ++i)
			{
				dispColors[i].a = fade;
				wordDisplays[i].color = dispColors[i];
			}
			yield return null;
		}

		for (i = 0; i < numDisplays; ++i)
			wordDisplays[i].gameObject.SetActive(false);
		yield break;
	}

	void AssignWordDisplayColor(int i, int end, Color color)
	{
		for (; i < end; ++i)
		{
			if (!wordDisplays[i].gameObject.activeSelf)
				break; // Don't bother iterating further.
			wordDisplays[i].color = color;
		}
	}


	// ------------------------
	// Other additional tidbits
	// ------------------------

	private static readonly Dictionary<string, string> __easterEggs = new Dictionary<string, string>()
	{
		{"DREAMCIPHER",  "!;089316,1;16b232,1;24c548,1;32d864,1;40e580,1;48ff96,1;56ffac,1;64ffc8,1;72ffe4,1;80ffff,1;88ffff,1"},

		{"LESBIANPRIDE", "!;d42c00,2;fd9855,2;ffffff,3;d161a2,3;a20161,2"},
		{"GAYPRIDE",     "!;333333,1;784f17,1;ff0000,1;ff8000,1;ffff00,1;00ff00,1;0020ff,1;c020ff,1"},
		{"BIPRIDE",      "!;d70270,3;734f96,1;0038a8,3"},
		{"TRANSPRIDE",   "!;5bcef5,2;f5a8b4,2;ffffff,2;f5a8b4,2;5bcef5,2"},
		{"TRANSRIGHTS",  "!;5bcef5,2;f5a8b4,2;ffffff,3;f5a8b4,2;5bcef5,2"},
		{"ENBYPRIDE",    "!;fef333,2;ffffff,2;9a58cf,3;333333,2"},
		{"GFLUIDPRIDE",  "!;ff75a2,2;ffffff,2;be18d6,3;333333,2;333ebd,2"},

		// These are actual words, and some of them are on the word list, so strikes still count.
		{"BISEXUALITY", "d70270,4;734f96,3;0038a8,4"},
		{"TRANSGENDER", "5bcef5,2;f5a8b4,2;ffffff,3;f5a8b4,2;5bcef5,2"},
		{"GENDERFLUID", "ff75a2,2;ffffff,2;be18d6,3;333333,2;333ebd,2"},
		{"NONBINARY",   "fef333,2;ffffff,3;9a58cf,2;333333,2"},
	};

	void CheckWordEasterEggs(out bool colorOverride, out bool skipStrike)
	{
		colorOverride = false;
		skipStrike = false;
		if (!__easterEggs.ContainsKey(wordInput))
			return;

		colorOverride = true;

		string[] coloring = __easterEggs[wordInput].Split(';');
		int len, cur = 0;
		Color newColor;

		foreach(string c in coloring)
		{
			if (c.Equals("!"))
			{
				skipStrike = true;
				continue;
			}
			len = (c[7] - '0');
			if (ColorUtility.TryParseHtmlString("#"+c.Substring(0,6), out newColor))
				AssignWordDisplayColor(cur, cur+len, newColor);
			cur += len;
		}
	}

	// ----------------------------
	// Word/Glyph/Answer generation
	// ----------------------------

	const int OUTLINE_OFFSET = 65;
	const int SEPARATOR_OFFSET = 64;

	public TextAsset wordList;

	string GetWord()
	{
		string[] words = JsonConvert.DeserializeObject<string[]>(wordList.text);
		string ourWord = words[RNG.Range(0, words.Length)].ToUpper();
		return ourWord;
	}

	Dictionary<char, int> GenerateAlphabet(char startPoint, out int outlineData, out List<string> retLog)
	{
		int nextGlyph = 0;
		List<string> log = new List<string>(); // work around CS1628

		// xorshift16 to generate a new 16-bit number after exhausting all bits
		int prngSeed = outlineData = RNG.Range(1, 65536);
		int prngShiftA = bombInfo.GetSerialNumberNumbers().FirstOrDefault() + 1;
		int prngShiftB = bombInfo.GetSerialNumberNumbers().LastOrDefault() + 1;
		int prngShiftC = (bombInfo.GetSerialNumberNumbers().Sum() % 15) + 1;
		Action prng = () => {
			prngSeed ^= (prngSeed << prngShiftA);
			prngSeed &= 0xFFFF;
			prngSeed ^= (prngSeed >> prngShiftB);
			prngSeed &= 0xFFFF;
			prngSeed ^= (prngSeed << prngShiftC);
			prngSeed &= 0xFFFF;
			log.Add(String.Format("* xorshift16({0},{1},{2}) => {3}",
				prngShiftA, prngShiftB, prngShiftC, Convert.ToString(prngSeed, 2).PadLeft(16, '0')));
		};
		log.Add("Generated binary numbers:");
		log.Add(String.Format("* Outlines to binary => {0}", Convert.ToString(prngSeed, 2).PadLeft(16, '0')));

		// The leftmost eight bits of the initial seed are used for starting position.
		nextGlyph = prngSeed >> 8;
		nextGlyph &= 0x3F;

		// Gets the next four-bit value from the PRNG seed, left-to-right (most-to-least)
		int iterSegment = 2;
		Func<int> step = () => {
			int ret = ((prngSeed >> (12 - iterSegment++ * 4) & 0xF));
			if ((iterSegment %= 4) == 0)
				prng(); // Call the prng function every four steps.
			return ret + 1;
		};

		// Do the first step outside the loop...
		nextGlyph = (nextGlyph + step()) & 0x3F;

		// Store the dictionary inverted while generating it
		// (faster lookups - checking if key exists is ~O(1), value is O(n))
		// This also ensures we never, ever duplicate glyphs, because it's literally impossible to.
		Dictionary<int, char> invertedDict = new Dictionary<int, char>() { { nextGlyph, startPoint } };

		// Now step until we return to the letter we started on.
		char c = (startPoint == 'Z' ? 'A' : (char)(startPoint + 1));
		for (; c != startPoint; c = (c == 'Z' ? 'A' : (char)(c + 1)))
		{
			nextGlyph += step();
			while (invertedDict.ContainsKey(nextGlyph &= 0x3F))
				++nextGlyph; // If not unique, advance one.

			invertedDict.Add(nextGlyph, c);
		}

		// Remove unnecessary xorshift log at the end -- replace with header for next section
		log[log.Count - 1] = "Glyph alphabet translation table:"; 

		// Log the translation table we've just made.
		for (int i = 0; i < 64; ++i)
		{
			if ((i & 7) == 0)
				log.Add("* ");

			log[log.Count - 1] += (invertedDict.ContainsKey(i)) ? invertedDict[i] : '-';
		}

		// Now, invert the dictionary the other way, and return it!
		Dictionary<char, int> alphabet = invertedDict.ToDictionary(x => x.Value, x => x.Key);
		retLog = log;
		return alphabet;
	}

	List<int> TranscribeWord(Dictionary<char, int> alphabet, string word)
	{
		return word.Select(c => alphabet[c]).ToList();
	}

	List<int> AddDecoyGlyphs(List<int> glyphs, Dictionary<char, int> alphabet)
	{
		List<int> unusedGlyphs = Enumerable.Range(0, SEPARATOR_OFFSET).Where(x => !alphabet.ContainsValue(x)).ToList();
		int newGlyph, timesToUse, rndVal;
		while (glyphs.Count < 15)
		{
			newGlyph = unusedGlyphs[RNG.Range(0, unusedGlyphs.Count)];

			// Weighted distribution for number of times the new decoy glyph appears
			rndVal = RNG.Range(0, 100);
			if (rndVal >= 95)      timesToUse = 4;
			else if (rndVal >= 85) timesToUse = 3;
			else if (rndVal >= 45) timesToUse = 2;
			else                   timesToUse = 1;
			if (glyphs.Count + timesToUse >= 15)
				timesToUse = 15 - glyphs.Count;

			do
				glyphs.Insert(RNG.Range(0, glyphs.Count + 1), newGlyph);
			while (--timesToUse > 0);

			unusedGlyphs.Remove(newGlyph);
		}
		return glyphs;
	}

	List<int> AddOutlineData(List<int> glyphs, int outlineData)
	{
		return glyphs.Select((g, i) => (outlineData & (0x8000 >> i)) == 0 ? g + OUTLINE_OFFSET : g).ToList();
	}

	void GenerateAnswer()
	{
		int outlineData;

		targetWord = GetWord();

		// Generate the glyph alphabet
		// Save the log, output it after logging all glyphs (looks better that way)
		char alphabetStartLetter = char.ToUpper(bombInfo.GetSerialNumberLetters().LastOrDefault());
		if (alphabetStartLetter == 0) // Letterless serial number?
			alphabetStartLetter = 'A'; // We'll just act like it ended with an A.

		List<string> alphabetLog = new List<string>();
		Dictionary<char, int> glyphAlphabet = GenerateAlphabet(alphabetStartLetter, out outlineData, out alphabetLog);

		// Use the alphabet we just created to transcribe the word, then add random decoy glyphs to pad to 15.
		// Then set outlined/filled shapes after adding the separator, for 16 bits of extra information displayed.
		List<int> transcription = TranscribeWord(glyphAlphabet, targetWord);
		transcription = AddDecoyGlyphs(transcription, glyphAlphabet);
		transcription.Add(SEPARATOR_OFFSET);
		transcription = AddOutlineData(transcription, outlineData);
		glyphList = transcription.ToArray();

		Debug.LogFormat("[Dreamcipher #{0}] The chosen word is '{1}'.", thisLogID, targetWord);
		string glyphs = "Displayed sequence of glyphs: ";
		for (int i = 0; i < 15; ++i)
		{
			if (glyphList[i] >= OUTLINE_OFFSET)
				glyphs += String.Format("{0}{1}, ", (char)((glyphList[i] - OUTLINE_OFFSET) % 8 + 'A'), ((glyphList[i] - OUTLINE_OFFSET) / 8) % 8 + 1);
			else
				glyphs += String.Format("{0}{1}*, ", (char)(glyphList[i] % 8 + 'A'), (glyphList[i] / 8) % 8 + 1);
		}
		Debug.LogFormat("[Dreamcipher #{0}] {1}SEP{2}", thisLogID, glyphs, glyphList[15] >= OUTLINE_OFFSET ? "" : "*");
		foreach (string s in alphabetLog)
			Debug.LogFormat("[Dreamcipher #{0}] {1}", thisLogID, s);

		// Just in case someone was playing with the module before it was on...
		moduleActive = true;
		wordInput = "";
		glyphDisplayAngle = 70f;
		inputOnChar = 0;

		inInputMode = buttonStuckDown;
		StartCoroutine(inInputMode ? DisplayInput() : DisplayGlyphs(true));
	}


	// ---------------
	// Switching modes
	// ---------------

	void EnterInputMode()
	{
		if (!moduleActive || moduleSolved)
			return;

		inInputMode = true;

		if (!wordInput.Equals("")) // Only needed if an easter egg is on screen
		{
			wordInput = "";
			wordClearCoroutine = StartCoroutine(FadeOutAndClearWordDisplay());
		}

		inputOnChar = 0;
		StartCoroutine(DisplayInput());
	}

	void ExitInputMode()
	{
		if (!moduleActive || moduleSolved)
			return;

		inInputMode = false;

		bool answerCorrect = wordInput.Equals(targetWord);
		bool skipStrike = wordInput.Equals("");
		bool colorOverride = false;

		if (!skipStrike)
			CheckWordEasterEggs(out colorOverride, out skipStrike);

		if (answerCorrect)
		{
			Debug.LogFormat("[Dreamcipher #{0}] SOLVE: Submitted '{1}'. Correct.", thisLogID, wordInput);

			bombAudio.PlaySoundAtTransform("Shutdown", gameObject.transform);
			if (!colorOverride)
				AssignWordDisplayColor(0, 15, new Color(0f, 1f, 0f, 1f));
			bombModule.HandlePass();
			StartCoroutine(WordDisplayShutdown());
			moduleSolved = true;
			return;
		}
		else if (!skipStrike)
		{
			Debug.LogFormat("[Dreamcipher #{0}] STRIKE: Submitted '{1}'. Wrong.", thisLogID, wordInput);

			bombAudio.PlaySoundAtTransform("Error", gameObject.transform);
			if (!colorOverride)
				AssignWordDisplayColor(0, 15, new Color(1f, 0f, 0f, 1f));
			bombModule.HandleStrike();
			wordInput = "";
			wordClearCoroutine = StartCoroutine(FadeOutAndClearWordDisplay());
		}
		else if (!wordInput.Equals(""))
			Debug.LogFormat("[Dreamcipher #{0}] Submitted '{1}'. No strike was given because that's an easter egg.", thisLogID, wordInput);

		// Yes, it's intentional that easter eggs that skip strikes stay on screen
		StartCoroutine(DisplayGlyphs(false));
	}

	// -----
	// The Dirty Work™
	// -----
	public KMSelectable centerButton;
	public KMSelectable inputButton;
	public KMSelectable backButton;
	public KMSelectable[] directionButtons;

	// Arrow buttons
	Coroutine holdCoroutine;

	IEnumerator InputHoldCoroutine(int amount)
	{
		inputOnChar = (inputOnChar + amount) % 26;
		yield return new WaitForSeconds(0.35f);
		while (true)
		{
			inputOnChar = (inputOnChar + amount) % 26;
			yield return new WaitForSeconds(0.09f);
		}
		// Unreachable
	}

	IEnumerator GlyphHoldCoroutine(float amount)
	{
		while (true)
		{
			staticDisplayTimer = Mathf.Max(staticDisplayTimer, 7f);
			glyphDisplayAngle += Time.deltaTime * amount;
			yield return null;
		}
		// Unreachable
	}

	bool DirectionPress(bool right)
	{
		// Disrupts mashing too much.
		//directionButtons[right ? 1 : 0].AddInteractionPunch(0.04f);
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, directionButtons[right ? 1 : 0].transform);
		directionButtons[right ? 1 : 0].GetComponentInParent<Animator>().Play("StandardDown", 0, 0);

		if (holdCoroutine != null)
			StopCoroutine(holdCoroutine);

		if (inInputMode)
			holdCoroutine = StartCoroutine(InputHoldCoroutine(right ? 25 : 1));
		else
			holdCoroutine = StartCoroutine(GlyphHoldCoroutine(right ? -120f : 120f));
		return false;
	}

	void DirectionRelease(bool right)
	{
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, directionButtons[right ? 0 : 1].transform);
		directionButtons[right ? 1 : 0].GetComponentInParent<Animator>().Play("StandardUp", 0, 0);

		if (holdCoroutine != null)
			StopCoroutine(holdCoroutine);
	}

	// Mode Switch (Center) button
	// Acts like one of those buttons that sticks down until you press it again
	bool buttonStuckDown = false;

	bool ModeSwitchPress()
	{
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, centerButton.transform);

		if ((buttonStuckDown = !buttonStuckDown) == true)
		{
			centerButton.GetComponentInParent<Animator>().Play("StickyDownFrom0", 0, 0);
			centerButton.AddInteractionPunch(0.1f);
			EnterInputMode();
		}
		else
		{
			centerButton.GetComponentInParent<Animator>().Play("StickyDownFrom1", 0, 0);
		}
		return false;
	}

	void ModeSwitchRelease()
	{
		if (!buttonStuckDown)
		{
			centerButton.AddInteractionPunch(0.08f);
			centerButton.GetComponentInParent<Animator>().Play("StickyUpTo0", 0, 0);
			bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, centerButton.transform);
			ExitInputMode();
		}
		else
		{
			centerButton.GetComponentInParent<Animator>().Play("StickyUpTo1", 0, 0);
			bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, centerButton.transform);			
		}
	}

	bool InputPress()
	{
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, inputButton.transform);
		inputButton.GetComponentInParent<Animator>().Play("StandardSingle", 0, 0);

		if (!moduleActive || moduleSolved)
			return false;

		// Not inputting: Force symbols to move again
		if (!inInputMode)
		{
			staticDisplayTimer = -1f;
			return false;
		}

		// In input mode
		if (wordInput.Length >= 15)
			return false;

		if (wordClearCoroutine != null)
		{
			StopCoroutine(wordClearCoroutine);
			wordClearCoroutine = null;
		}

		wordInput += "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[inputOnChar];
		UpdateWordDisplay();
		return false;
	}

	bool DeletePress()
	{
		bombAudio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, backButton.transform);
		backButton.GetComponentInParent<Animator>().Play("StandardSingle", 0, 0);

		if (!moduleActive || moduleSolved)
			return false;

		// Not inputting: Force symbols to stop
		if (!inInputMode)
		{
			staticDisplayTimer = Mathf.Infinity;
			return false;
		}

		// In input mode
		if (wordInput.Length <= 1)
			wordInput = "";
		else
			wordInput = wordInput.Remove(wordInput.Length - 1);
		UpdateWordDisplay();
		return false;
	}

	void Awake()
	{
		thisLogID = ++globalLogID;

		centerButton.OnInteract += ModeSwitchPress;
		centerButton.OnInteractEnded += ModeSwitchRelease;
		directionButtons[0].OnInteract += delegate() { return DirectionPress(false); };
		directionButtons[1].OnInteract += delegate() { return DirectionPress(true); };
		directionButtons[0].OnInteractEnded += delegate() { DirectionRelease(false); };
		directionButtons[1].OnInteractEnded += delegate() { DirectionRelease(true); };
		inputButton.OnInteract += InputPress;
		backButton.OnInteract += DeletePress;

		bombModule.OnActivate += GenerateAnswer;
	}


	// -----
	// Twitch Plays support
	// -----
#pragma warning disable 414
	private bool TwitchShouldCancelCommand = false;
	private readonly string TwitchHelpMessage = @"View all glyphs with '!{0} cycle'; submit an answer with '!{0} submit ANSWERING'.";
#pragma warning restore 414

	public IEnumerator ProcessTwitchCommand(string command)
	{
		if (Regex.IsMatch(command, @"^\W*cycle\W*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;

			// Where normally I would yield buttons to TP, I want more precise control of response times here.
			if (inInputMode)
			{
				// This should only happen in Interactive mode anyway.
				wordInput = ""; // Just act like we mashed the back button a ton.
				centerButton.OnInteract();
				yield return new WaitForSeconds(0.1f);
				centerButton.OnInteractEnded();
			}
			backButton.OnInteract();

			for (float targetAngle = 246f; targetAngle < 800f; targetAngle += 180)
			{
				float curTime = 0f;

				directionButtons[0].OnInteract();
				// Why doesn't this work in a WaitWhile or WaitUntil? What is Unity doing‽
				while (!(TwitchShouldCancelCommand || (glyphDisplayAngle >= targetAngle && glyphDisplayAngle <= targetAngle + (Time.deltaTime*240f))))
					yield return null;
				directionButtons[0].OnInteractEnded();

				if (!TwitchShouldCancelCommand)
					glyphDisplayAngle = targetAngle;
				while (!(TwitchShouldCancelCommand || (curTime += Time.deltaTime) >= 7.5f))
					yield return null;

				if (TwitchShouldCancelCommand)
				{
					inputButton.OnInteract();
					yield return "sendtochat Sorry, {0}, the cycle was aborted due to a cancel request.";
					yield return "cancelled";
					yield break;
				}
			}
			inputButton.OnInteract();
			yield break;
		}

		Match mt;
		if ((mt = Regex.Match(command, @"^\W*submit\W+([A-Z]+)\W*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
		{
			string input = mt.Groups[1].ToString().ToUpper();
			float angleTarget;

			if (input.Length > 15)
			{
				yield return String.Format("sendtochaterror Pardon me asking, but how did you supposedly get a {0}-letter word out of 15 symbols?", input.Length);
				yield break;
			}

			yield return null;

			if (!inInputMode)
			{
				centerButton.OnInteract();
				yield return new WaitForSeconds(0.1f);
				centerButton.OnInteractEnded();
			}
			else
				wordInput = ""; // Just act like we mashed the back button a ton.

			// To speed up TP, we manually set the character, and just wait until it scrolls close enough on screen to input it.
			for (int i = 0; i < input.Length; ++i)
			{
				inputOnChar = input[i] - 'A';
				angleTarget = 492f + (inputOnChar * 24f);

				yield return new WaitUntil(() => Mathf.Abs(inputDisplayAngle-angleTarget) < 10f);
				inputButton.OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			centerButton.OnInteract();
			yield return new WaitForSeconds(0.1f);
			centerButton.OnInteractEnded();
		}
		yield break;
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		if (moduleSolved)
			yield break;

		Debug.LogFormat("[Dreamcipher #{0}] Force solve requested by Twitch Plays.", thisLogID);
		yield return ProcessTwitchCommand(String.Format("submit {0}", targetWord));
	}
}
