using System;
using System.Linq;
using D = UnityEngine.Debug;
using R = UnityEngine.Random;
using C = System.Convert;
using E = System.Text.UTF8Encoding;

public class EncLogic
{
	private static string[] OrPerhapsYouDecompiledThis = {
		"Just hoping you aren't looking at this through a hex editor ... are you?",

		//"Oh? What's this? You expected something to be in the logs? How cute.",
		//"Logging for this module is not available. Duh.",
		//"We're sorry, your log could not be completed at this time. Please check your answer and try your log again.",
		//"Oh, it's a Dreamcipher! Time to yeet the bomb.",
		//"Have you figured it out yet?",
		"TzTPBHVDhegnuQiOhkvNfiCs264CK+HgZbEdrEYBzd5tsh0NlnOdQHS3CExWAaXcIDoarAJjvc5znwgJ9ruAxnU6WcU=",
		"TLfZ7JZznUBmt5wER0Ol5iC224xXY5VAabkIzfajgMJ2sFqNFhOxyi4QEa6GcQ==",
		"V7LJTlYBzd5yOV6FAsu96nIQG+12AY3edTYZBOZ70UBisghs9mvB2GU6WYwCC9FAdDRabgKjpdplFwgKxiuF5mUQ2A1WG61AebddTgILueZ3spwEFnORQHQ5XgSXe9XkIDbb7AILncJpN4s=",
		"TzQLBJajnOYgsAiIJyuF2mO0HA1Wk4RAVLRbrAKjvUB5slmOAqOhyiAx260mcQ==",
		"SLCdrALLveogM1rsV5OVyCC0HQT2q9FAebId5w==",
		"TzTPBHVDhegnuQiOhkvNfiCs264CK+HgZbEdrEYBzd5tsh0NlnOdQHS3CExWAaXcIDoarAJjvc5znwgJ9ruAxnU6WcU=",
		"TLfZ7JZznUBmt5wER0Ol5iC224xXY5VAabkIzfajgMJ2sFqNFhOxyi4QEa6GcQ==",
		"V7LJTlYBzd5yOV6FAsu96nIQG+12AY3edTYZBOZ70UBisghs9mvB2GU6WYwCC9FAdDRabgKjpdplFwgKxiuF5mUQ2A1WG61AebddTgILueZ3spwEFnORQHQ5XgSXe9XkIDbb7AILncJpN4s=",
		"TzQLBJajnOYgsAiIJyuF2mO0HA1Wk4RAVLRbrAKjvUB5slmOAqOhyiAx260mcQ==",
		"SLCdrALLveogM1rsV5OVyCC0HQT2q9FAebId5w==",
		"TzTPBHVDhegnuQiOhkvNfiCs264CK+HgZbEdrEYBzd5tsh0NlnOdQHS3CExWAaXcIDoarAJjvc5znwgJ9ruAxnU6WcU=",
		"TLfZ7JZznUBmt5wER0Ol5iC224xXY5VAabkIzfajgMJ2sFqNFhOxyi4QEa6GcQ==",
		"V7LJTlYBzd5yOV6FAsu96nIQG+12AY3edTYZBOZ70UBisghs9mvB2GU6WYwCC9FAdDRabgKjpdplFwgKxiuF5mUQ2A1WG61AebddTgILueZ3spwEFnORQHQ5XgSXe9XkIDbb7AILncJpN4s=",
		"TzQLBJajnOYgsAiIJyuF2mO0HA1Wk4RAVLRbrAKjvUB5slmOAqOhyiAx260mcQ==",
		"SLCdrALLveogM1rsV5OVyCC0HQT2q9FAebId5w==",

		//"Keep trying, maybe one of these strings has a hint.",
		//"By the way, this module says 'TRANSRIGHTS'."
		//"Hopefully you've realized there's multiple of these strings by now."
		"S7JZDgKjyfJpN9mFAmuF8mKyCO3mK4DeZhAdDVablUBzOpwt5jvNQGiw3AQWAaHSbjqL",
		"QrwIjoYrgO5hvAsER0Ol5iC224xXY5VAc7BebgI5UaRBJ9RKlDohqFOTiw==",
		"SLccrGarsdh5EF7tVznZyiA3246WG5XIIDFeBOZ73UB0NFiOAqOhynKyCCwnK4DadTYdLQdjlUBvMwiOhivNyiC5HU6Wc53mLg==",
		"S7JZDgKjyfJpN9mFAmuF8mKyCO3mK4DeZhAdDVablUBzOpwt5jvNQGiw3AQWAaHSbjqL",
		"QrwIjoYrgO5hvAsER0Ol5iC224xXY5VAc7BebgI5UaRBJ9RKlDohqFOTiw==",
		"SLccrGarsdh5EF7tVznZyiA3246WG5XIIDFeBOZ73UB0NFiOAqOhynKyCCwnK4DadTYdLQdjlUBvMwiOhivNyiC5HU6Wc53mLg==",

		//"Looking for a clue? Look for the manual.",
		"TLfbbZZznUBmt5wEFgGN2HWyzwTEe73WIDPbTgKjocogtljNVwuxXA==",
		"TLfbbZZznUBmt5wEFgGN2HWyzwTEe73WIDPbTgKjocogtljNVwuxXA==",

		//"If it's not on Timwi's repo, then ... where else would it be?",
		"STMILUc5zUButx0E9nOAqGm23S1ym4DkZTjbhQKjocpuEIvF4gHd0GU5WQRWY83KILvbrsYjgNJ0EJis8w==",

		//"[13]n3EuozHhpKI0MF5xo2pioJShqJSfpl9RpzIuoJAcpTuypv5bqT1f",
		"W5jMq+aZFepvPRINB1olYE0jTQ/2kcHSbyXUDRdSTcxwNk5KB9Ml6m8lUGwHotXycDtNTBeixMw=",
	};

	private static int r = 5;
	private static int Z2V0cmFuZG9t() { return R.Range(1,r); }

	private string G="";

	public EncLogic(int _)
	{
		G=String.Format(new E().GetString(C.FromBase64String("W0RyZWFtY2lwaGVyICN7MH1dIA==")),_);
		r = R.Range(15, OrPerhapsYouDecompiledThis[0][0]-'0');
	}

	public void dGF1bnQK()
	{
		byte[] a=C.FromBase64String(OrPerhapsYouDecompiledThis[Z2V0cmFuZG9t()]);
		for(int A=0;A<a.Length;++A)a[A]=(byte)(a[A]<<(A&7)|a[A]>>(8-(A&7)));
		string g=new E().GetString(a);
		D.LogFormat("{0}{1}",G,g);
	}
}