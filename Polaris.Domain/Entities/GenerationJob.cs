namespace Polaris.Domain.Entities
{
    /// <summary>
    /// Domain entity representing a code generation job.
    /// Tracks the full lifecycle of an AI code generation request,
    /// from initial submission through processing to completion or failure.
    /// 
    /// Status flow: Pending → Processing → Completed | Failed | Cancelled
    /// </summary>
    public class GenerationJob
    {
        /// <summary>Unique identifier for the generation job.</summary>
        public Guid Id { get; set; }

        /// <summary>Optional Conversation ID if this job is part of a chat session.</summary>
        public Guid? ConversationId { get; set; }

        /// <summary>The user who submitted the generation request.</summary>
        public Guid UserId { get; set; }

        /// <summary>The user's prompt describing what code to generate.</summary>
        public string Prompt { get; set; } = string.Empty;

        /// <summary>
        /// Current job status. Valid values: "Pending", "Processing", "Completed", "Failed", "Cancelled".
        /// </summary>
        public string Status { get; set; } = "Pending";

        /// <summary>The generated code result (may be partial during processing).</summary>
        public string? Result { get; set; }

        /// <summary>Error message if the job failed.</summary>
        public string? Error { get; set; }

        /// <summary>Progress percentage (0-100). Updated periodically during processing.</summary>
        public int Progress { get; set; }

        /// <summary>When the job was originally created/submitted.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>When the background processor started working on the job.</summary>
        public DateTime? StartedAt { get; set; }

        /// <summary>When the job reached a terminal state (Completed/Failed/Cancelled).</summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>When the job's result was last updated in the database.</summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// The Hangfire background job ID, used to monitor the processor's health
        /// and detect unexpected termination.
        /// </summary>
        public string? HangfireJobId { get; set; }

        /// <summary>
        /// User's current intent for this job. Set to "cancelled" by the cancel endpoint,
        /// which the background processor checks periodically to stop gracefully.
        /// Default value is "active".
        /// </summary>
        public string? UserIntent { get; set; } = "active";
    }
}
