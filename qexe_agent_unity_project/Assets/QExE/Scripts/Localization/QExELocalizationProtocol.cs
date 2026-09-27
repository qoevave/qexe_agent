namespace QExE.Localization
{
    /// <summary>
    /// OSC addresses for the localization method. Keep in sync with
    /// qexe_controller/src/code/method_Localization.js and docs/protocol.md.
    /// </summary>
    public static class QExELocalizationProtocol
    {
        // Unity -> Max
        /// <summary>Participant pressed Start. No arguments.</summary>
        public const string Start = "/qexe/loc/start";

        /// <summary>
        /// Pointing response: i:trial f:azimuth f:elevation f:azimuthHead f:elevationHead
        /// f:headYaw f:headPitch f:rtMs s:isoTimestamp s:source("pointer"|"dial")
        /// i:hasPreliminary f:preliminaryAzimuth f:preliminaryTimeMs f:markTimeMs
        /// f:onsetHeadYaw f:onsetHeadPitch f:maxAbsYawDuringStimulus f:maxAbsPitchDuringStimulus
        /// (markTimeMs = when the submitted direction was marked, ms from the trial start;
        ///  head deviation is tracked from stimulus onset until the response window opens)
        /// </summary>
        public const string Response = "/qexe/loc/response";

        /// <summary>Participant is aligned (reply to Ready): i:trial f:headYaw f:headPitch</summary>
        public const string Aligned = "/qexe/loc/aligned";

        // Max -> Unity
        /// <summary>
        /// Next trial is ready and waits for head alignment (only when requireAlignment is on):
        /// i:trial i:trialCount f:toleranceDeg i:holdMs s:stimulusId
        /// </summary>
        public const string Ready = "/qexe/loc/ready";

        /// <summary>Stimulus onset: i:trial i:trialCount s:stimulusId</summary>
        public const string Trial = "/qexe/loc/trial";

        /// <summary>The response window is open: i:trial</summary>
        public const string Respond = "/qexe/loc/respond";

        /// <summary>A block starts: s:"training"|"test" i:trialCount</summary>
        public const string Mode = "/qexe/loc/mode";

        /// <summary>Training only, after a response: the correct direction. i:trial f:targetAzimuth f:targetElevation</summary>
        public const string Feedback = "/qexe/loc/feedback";

        /// <summary>All trials of the block are done: i:trialCount</summary>
        public const string Complete = "/qexe/loc/complete";
    }
}
