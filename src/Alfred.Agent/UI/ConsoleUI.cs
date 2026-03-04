using Spectre.Console;

namespace Alfred.Agent.UI;

/// <summary>Centralises all console rendering so colours and styles stay consistent.</summary>
internal static class ConsoleUI
{
	// ── Styles ────────────────────────────────────────────────────────────────

	private const string UserStyle = "bold yellow";
	private const string AlfredStyle = "bold steelblue1";
	private const string TelegramStyle = "bold mediumpurple1";
	private const string TranscriptStyle = "dim italic grey";
	private const string ErrorStyle = "bold red";

	// ── Banner ────────────────────────────────────────────────────────────────

	public static void ShowBanner()
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Rule("[bold steelblue1]Alfred is ready[/]").RuleStyle("grey").LeftJustified());
		AnsiConsole.MarkupLine("[grey]Type your message or [bold]exit[/] to quit.[/]");
		AnsiConsole.WriteLine();
	}

	// ── Spinner ───────────────────────────────────────────────────────────────

	private static readonly Spinner[] _knownSpinners =
	[
		Spinner.Known.Default,
		Spinner.Known.Ascii,
		Spinner.Known.Dots,
		Spinner.Known.Dots2,
		Spinner.Known.Dots3,
		Spinner.Known.Dots4,
		Spinner.Known.Dots5,
		Spinner.Known.Dots6,
		Spinner.Known.Dots7,
		Spinner.Known.Dots8,
		Spinner.Known.Dots9,
		Spinner.Known.Dots10,
		Spinner.Known.Dots11,
		Spinner.Known.Dots12,
		Spinner.Known.Dots13,
		Spinner.Known.Dots14,
		Spinner.Known.Dots8Bit,
		Spinner.Known.DotsCircle,
		Spinner.Known.Sand,
		Spinner.Known.Line,
		Spinner.Known.Line2,
		Spinner.Known.Pipe,
		Spinner.Known.SimpleDots,
		Spinner.Known.SimpleDotsScrolling,
		Spinner.Known.Star,
		Spinner.Known.Star2,
		Spinner.Known.Flip,
		Spinner.Known.Hamburger,
		Spinner.Known.GrowVertical,
		Spinner.Known.GrowHorizontal,
		Spinner.Known.Balloon,
		Spinner.Known.Balloon2,
		Spinner.Known.Noise,
		Spinner.Known.Bounce,
		Spinner.Known.BoxBounce,
		Spinner.Known.BoxBounce2,
		Spinner.Known.Triangle,
		Spinner.Known.Binary,
		Spinner.Known.Arc,
		Spinner.Known.Circle,
		Spinner.Known.SquareCorners,
		Spinner.Known.CircleQuarters,
		Spinner.Known.CircleHalves,
		Spinner.Known.Squish,
		Spinner.Known.Toggle,
		Spinner.Known.Toggle2,
		Spinner.Known.Toggle3,
		Spinner.Known.Toggle4,
		Spinner.Known.Toggle5,
		Spinner.Known.Toggle6,
		Spinner.Known.Toggle7,
		Spinner.Known.Toggle8,
		Spinner.Known.Toggle9,
		Spinner.Known.Toggle10,
		Spinner.Known.Toggle11,
		Spinner.Known.Toggle12,
		Spinner.Known.Toggle13,
		Spinner.Known.Arrow,
		Spinner.Known.Arrow2,
		Spinner.Known.Arrow3,
		Spinner.Known.BouncingBar,
		Spinner.Known.BouncingBall,
		Spinner.Known.Smiley,
		Spinner.Known.Monkey,
		Spinner.Known.Hearts,
		Spinner.Known.Clock,
		Spinner.Known.Earth,
		Spinner.Known.Material,
		Spinner.Known.Moon,
		Spinner.Known.Runner,
		Spinner.Known.Pong,
		Spinner.Known.Shark,
		Spinner.Known.Dqpb,
		Spinner.Known.Weather,
		Spinner.Known.Christmas,
		Spinner.Known.Grenade,
		Spinner.Known.Point,
		Spinner.Known.Layer,
		Spinner.Known.BetaWave,
		Spinner.Known.FingerDance,
		Spinner.Known.FistBump,
		Spinner.Known.SoccerHeader,
		Spinner.Known.Mindblown,
		Spinner.Known.Speaker,
		Spinner.Known.OrangePulse,
		Spinner.Known.BluePulse,
		Spinner.Known.OrangeBluePulse,
		Spinner.Known.TimeTravel,
		Spinner.Known.Aesthetic,
		Spinner.Known.DwarfFortress,
	];

	private static Spinner RandomSpinner() => _knownSpinners[Random.Shared.Next(_knownSpinners.Length)];

	/// <summary>
	/// Runs <paramref name="work"/> while displaying an animated spinner with
	/// <paramref name="status"/> as the label, then returns the result.
	/// </summary>
	public static Task<T> RunWithSpinnerAsync<T>(string status, Func<Task<T>> work)
		=> AnsiConsole.Status()
			.Spinner(RandomSpinner())
			.SpinnerStyle(Style.Parse(AlfredStyle))
			.StartAsync(status, _ => work());

	// ── Prompt helpers ────────────────────────────────────────────────────────

	/// <summary>Writes the "You »" prompt and returns the trimmed input line.</summary>
	public static string? PromptUser()
	{
		AnsiConsole.Markup($"\n[{UserStyle}]You »[/] ");
		return Console.ReadLine();
	}

	/// <summary>Writes the "Alfred »" label on its own line before streaming the reply.</summary>
	public static void BeginAlfredReply()
		=> AnsiConsole.Markup($"[{AlfredStyle}]Alfred »[/] ");

	public static void WriteAlfredReply(string reply)
	{
		BeginAlfredReply();
		AnsiConsole.MarkupLine(Markup.Escape(reply));
	}

	// ── Telegram ──────────────────────────────────────────────────────────────

	public static void ShowTelegramTextMessage(string text)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[{TelegramStyle}]Telegram »[/] {Markup.Escape(text)}");
	}

	public static void ShowVoiceTranscribing()
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[{TelegramStyle}]Telegram »[/] [grey]🎤 Voice message received, transcribing…[/]");
	}

	public static void ShowTranscript(string text)
		=> AnsiConsole.MarkupLine($"[{TranscriptStyle}]  ↳ Transcribed:[/] [{TranscriptStyle}]{Markup.Escape(text)}[/]");

	// ── Scheduler ─────────────────────────────────────────────────────────────

	public static void ShowScheduledPrompt(string name, string prompt)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Rule($"[bold steelblue1]⏰ {Markup.Escape(name)}[/]").RuleStyle("grey").LeftJustified());
		AnsiConsole.MarkupLine($"[{TranscriptStyle}]  Prompt: {Markup.Escape(prompt)}[/]");
	}

	// ── Errors ────────────────────────────────────────────────────────────────

	public static void ShowError(string context, string message)
		=> AnsiConsole.MarkupLine($"[{ErrorStyle}][{Markup.Escape(context)}][/] [red]{Markup.Escape(message)}[/]");

	// ── Goodbye ───────────────────────────────────────────────────────────────

	public static void ShowGoodbye()
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(new Rule("[grey]Goodbye[/]").RuleStyle("grey").LeftJustified());
	}
}
