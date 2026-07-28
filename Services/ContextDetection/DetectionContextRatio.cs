using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
// Microsoft.ML.OnnxRuntime


namespace Scheder.ContextDetection
{
    
    public static partial class TextNormalizer
    {
        private static readonly (Regex Pattern, string Replacement)[] TypoFixes =
        {
            (MyRegex(), "завтра"),
            (MyRegex1(), "завтра"),
        };


        private static readonly Regex NonWord = new Regex(@"[^\w\s]", RegexOptions.Compiled);
        private static readonly Regex MultiSpace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;

            s = s.ToLowerInvariant();
            s = s.Replace('ё', 'е');

            foreach (var (pattern, replacement) in TypoFixes)
                s = pattern.Replace(s, replacement);

            s = NonWord.Replace(s, " ");
            s = MultiSpace.Replace(s, " ").Trim();
            return s;
        }

        [GeneratedRegex(@"\bзатр[аи]\b", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"\bзавтро\b", RegexOptions.Compiled)]
        private static partial Regex MyRegex1();
    }
    
    public static partial class NegationDetector
    {
        private static readonly Regex[] NegPatterns =
        {
            MyRegex7(),
            MyRegex6(),
            MyRegex5(),
            MyRegex4(),
            MyRegex3(),
            MyRegex2(),
            MyRegex1(),
            MyRegex()
        };

        public static bool HasStrongNegation(string text)
        {
            var t = TextNormalizer.Normalize(text);
            return NegPatterns.Any(p => p.IsMatch(t));
        }

        [GeneratedRegex(@"завтра\s+нет\s+пар", RegexOptions.Compiled)]
        private static partial Regex MyRegex();
        [GeneratedRegex(@"сегодня\s+нет\s+пар", RegexOptions.Compiled)]
        private static partial Regex MyRegex1();
        [GeneratedRegex(@"не\s+буду.*пар", RegexOptions.Compiled)]
        private static partial Regex MyRegex2();
        [GeneratedRegex(@"не\s+хочу.*пар", RegexOptions.Compiled)]
        private static partial Regex MyRegex3();
        [GeneratedRegex(@"пар\s+нет", RegexOptions.Compiled)]
        private static partial Regex MyRegex4();
        [GeneratedRegex(@"нет\s+пар", RegexOptions.Compiled)]
        private static partial Regex MyRegex5();
        [GeneratedRegex(@"пар[ауеы]?\s+не\s+буд\w+", RegexOptions.Compiled)]
        private static partial Regex MyRegex6();
        [GeneratedRegex(@"не\s+буд\w+.*пар", RegexOptions.Compiled)]
        private static partial Regex MyRegex7();
    }
    
    public sealed class ScheduleClassifier : IDisposable
    {
        private readonly InferenceSession _session;
        private readonly string _inputName;
        private readonly string _outputName;

        private ScheduleClassifier(byte[] onnxBytes, string? inputName = null, string? outputName = null)
        {
            _session = new InferenceSession(onnxBytes);
            
            _inputName = inputName ?? _session.InputMetadata.Keys.First();
            _outputName = outputName ?? _session.OutputMetadata.Keys
                .First(n => n.Contains("prob", StringComparison.OrdinalIgnoreCase));
        }
        
        public ScheduleClassifier(string modelPath, string? inputName = null, string? outputName = null)
        {
            _session = new InferenceSession(modelPath);
    
            _inputName = inputName ?? _session.InputMetadata.Keys.First();
            _outputName = outputName ?? _session.OutputMetadata.Keys
                .First(n => n.Contains("prob", StringComparison.OrdinalIgnoreCase));
        }
        
        public static ScheduleClassifier LoadEmbedded(string resourceName, System.Reflection.Assembly? assembly = null)
        {
            assembly ??= System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(resourceName)
                               ?? throw new InvalidOperationException($"Ресурс не найден: {resourceName}");
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return new ScheduleClassifier(ms.ToArray());
        }

        public float PredictProbe(string text)
        {
            var normalized = TextNormalizer.Normalize(text);

            var inputTensor = new DenseTensor<string>(new[] { normalized }, [1, 1]);
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
            };

            using var results = _session.Run(inputs);
            var probes = results.First(r => r.Name == _outputName).AsEnumerable<float>().ToArray();
            
            return probes.Length > 1 ? probes[1] : probes[0];
        }

        public bool IsScheduleQuery(string text, float threshold)
        {
            if (NegationDetector.HasStrongNegation(text))
                return false;

            return PredictProbe(text) >= threshold;
        }

        public void Dispose() => _session.Dispose();
    }


    public static class DetectionContextRatio
    {
        public const float DefaultThreshold = 0.625f;
        private static readonly Lock Lock = new();
        private static ScheduleClassifier? _instance;
        private static string _onnxPath = "dataset.onnx";
        
        public static void Init(string? onnxPath = null)
        {
            lock (Lock)
            {
                _onnxPath = onnxPath ?? _onnxPath;
                _instance?.Dispose();
                _instance = null;
            }
        }
        
        public static void InitEmbedded(string resourceName, System.Reflection.Assembly? assembly = null)
        {
            lock (Lock)
            {
                _instance?.Dispose();
                _instance = ScheduleClassifier.LoadEmbedded(resourceName, assembly);
            }
        }
        
 
        private static ScheduleClassifier Instance
        {
            get
            {
                if (_instance is not null) return _instance;
                lock (Lock)
                {
                    _instance ??= new ScheduleClassifier(_onnxPath);
                }
                return _instance;
            }
        }
        
        public static float GetRatio(string incomingText)
        {
            if (NegationDetector.HasStrongNegation(incomingText))
                return 0f;
 
            return Instance.PredictProbe(incomingText);
        }
        
        public static bool IsRunAllowed(string incomingText, float threshold = DefaultThreshold)
            => Instance.IsScheduleQuery(incomingText, threshold);
    }
    

    // Пример использования:
    //
    // 
    // bool isSchedule = clf.IsScheduleQuery("какие завтра пары?");
    // float p = clf.PredictProba("какие завтра пары?");
}