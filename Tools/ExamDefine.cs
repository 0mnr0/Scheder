using System.Text.Json.Serialization;

namespace Scheder.Tools;

public class ExamDefine {
    
    public class StudentExams {
        
        [JsonPropertyName("teacher")]
        public string Teacher { get; set; } = string.Empty;
        
        [JsonPropertyName("date")]  // [?] yyyy-mm-dd
        public string? Date { get; set; } = null;
        
        [JsonPropertyName("exam_id")]
        public long ExamId { get; set; } = 0;
        
        [JsonPropertyName("spec")]
        public string Spec { get; set; } = string.Empty;
        
        [JsonPropertyName("attestation_type")]
        public int AttestationType { get; set; } = 0;
        
        [JsonPropertyName("subject_source")]
        public int SubjectSource { get; set; } = 0;
        
        [JsonPropertyName("subject_id")]
        public long SubjectId { get; set; } = 0;
    }
    
    public class FutureExams {
        [JsonPropertyName("spec")]
        public string Spec { get; set; } = string.Empty;

        [JsonPropertyName("date")] // yyyy-mm-dd
        public string Date { get; set; } = string.Empty;
        
        [JsonPropertyName("attestation_type")]
        public int AttestationType { get; set; } = 0;
        
        [JsonPropertyName("subject_source")]
        public int SubjectSource { get; set; } = 0;
        
        [JsonPropertyName("subject_id")]
        public long SubjectId { get; set; } = 0;
    }
    
}