namespace QExE.Localization
{
    /// <summary>
    /// OSC addresses for the paired (A/B) localization method. Keep in sync with
    /// qexe_controller/src/code/method_PairedLocalization.js. Separate from
    /// <see cref="QExELocalizationProtocol"/> so the two methods never interfere.
    /// Sides are the strings "A" and "B".
    /// </summary>
    public static class QExEPairedLocalizationProtocol
    {
        // Unity -> Max
        /// <summary>Participant pressed Start. No arguments.</summary>
        public const string Start = "/qexe/pair/start";

        /// <summary>Participant pressed A or B: i:trial s:side. While a comparison playback runs, this switches the audible side.</summary>
        public const string Play = "/qexe/pair/play";

        /// <summary>Participant is aligned (reply to Ready): i:trial f:headYaw f:headPitch</summary>
        public const string Aligned = "/qexe/pair/aligned";

        /// <summary>
        /// Head orientation during a playback, sent when the playback ended:
        /// i:trial i:playIndex f:onsetHeadYaw f:onsetHeadPitch f:maxAbsYaw f:maxAbsPitch
        /// </summary>
        public const string PlayHead = "/qexe/pair/playhead";

        /// <summary>
        /// Answer for one side: i:trial s:side f:azimuth f:elevation f:azimuthHead f:elevationHead
        /// f:headYaw f:headPitch f:rtMs s:isoTimestamp s:source("pointer"|"dial") f:markTimeMs
        /// (rtMs / markTimeMs: ms since the answer for this side could first be given)
        /// </summary>
        public const string Response = "/qexe/pair/response";

        // Max -> Unity
        /// <summary>A block starts: s:"training"|"test" i:pairCount</summary>
        public const string Mode = "/qexe/pair/mode";

        /// <summary>A pair begins (A active, B inactive): i:trial i:pairCount s:itemId</summary>
        public const string Pair = "/qexe/pair/pair";

        /// <summary>Pair state: i:trial i:aEnabled i:bEnabled i:answerOpen s:answerSide</summary>
        public const string State = "/qexe/pair/state";

        /// <summary>A requested playback waits for head alignment: i:trial s:side f:toleranceDeg i:holdMs</summary>
        public const string Ready = "/qexe/pair/ready";

        /// <summary>Stimulus onset: i:trial s:side i:playIndex</summary>
        public const string Playing = "/qexe/pair/playing";

        /// <summary>While comparing (answering B): the audible side was switched. i:trial s:side</summary>
        public const string Selected = "/qexe/pair/selected";

        /// <summary>Stimulus ended: i:trial s:side (audible at the end) i:playIndex</summary>
        public const string Played = "/qexe/pair/played";

        /// <summary>An answer was accepted and is now locked: i:trial s:side</summary>
        public const string Answered = "/qexe/pair/answered";

        /// <summary>All pairs of the block are done: i:pairCount</summary>
        public const string Complete = "/qexe/pair/complete";

        /// <summary>The block was reset (operator Abort, or Start rejected): back to waiting for Start. No arguments.</summary>
        public const string Abort = "/qexe/pair/abort";
    }
}
