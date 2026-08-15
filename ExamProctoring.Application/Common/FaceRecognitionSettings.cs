namespace ExamProctoring.Application.Common
{
    /// Configuration for backend reference-face enrolment.
    ///
    /// Every value has a working default, so the feature runs with no configuration at all.
    /// The defaults are the OpenCV reference values for YuNet; they are exposed only so a
    /// deployment can adjust them without a rebuild, not because they are expected to change.
    public class FaceRecognitionSettings
    {
        public const string SectionName = "FaceRecognition";

        /// Directory holding the two ONNX models, relative to the application base directory
        /// (AppContext.BaseDirectory). Relative on purpose: an absolute path would work on the
        /// developer's machine and break on the Plesk/IIS host, which is exactly the class of
        /// deployment failure this project has already hit once.
        public string ModelDirectory { get; set; } = "Models/Face";

        public string DetectorModelFileName { get; set; } = "face_detection_yunet_2023mar.onnx";

        public string RecognizerModelFileName { get; set; } = "face_recognition_sface_2021dec.onnx";

        /// YuNet confidence floor. The OpenCV reference default.
        public float DetectionScoreThreshold { get; set; } = 0.9f;

        /// Non-maximum suppression IoU. The OpenCV reference default.
        public float DetectionNmsThreshold { get; set; } = 0.3f;

        /// Detections kept before NMS. Generous: the enrolment rule needs an honest count of
        /// how many faces are present, so this must not silently truncate a group photo to one.
        public int DetectionTopK { get; set; } = 5000;

        /// Longest side an administrative photo is scaled to before detection.
        ///
        /// Downscaling is deliberate rather than merely economical: SFace always aligns to a
        /// 112x112 chip, and a face cropped from a 4000px portrait is resampled far harder in
        /// one step than one cropped from a 1024px image. It also brings the reference closer
        /// in scale to the client's camera frames, which is the comparison that has to work.
        public int MaxImageDimension { get; set; } = 1024;
    }
}
