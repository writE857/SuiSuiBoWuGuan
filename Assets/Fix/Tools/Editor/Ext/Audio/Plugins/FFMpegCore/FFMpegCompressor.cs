using System;
using System.IO;
using System.Linq;
using FFMpegCore;
using UnityEngine;

namespace Fix.Editor
{
    public class FFMpegCompressor : IAudioCompressor
    {
        private static TimeSpan MinLength;

        public void Init()
        {
            var info = new DirectoryInfo(Application.dataPath);
            var folder = info.GetDirectories("FFMpegCore", SearchOption.AllDirectories).First();
            GlobalFFOptions.Configure(options => options.BinaryFolder = folder.FullName);
            MinLength = AudioClipEditorUtils.MinLength;
        }

        public bool Compress(string path, string outputPath, float percentage)
        {
            return HandleByFFmpeg(path, outputPath, percentage);
        }

        private static bool HandleByFFmpeg(string path, string outputPath, float percentage)
        {
            var analysis = FFProbe.Analyse(path);
            var duration = new TimeSpan((long) (analysis.Duration.Ticks * percentage));
            return FFMpegArguments
                .FromFileInput(Path.GetFullPath(path))
                .OutputToFile(Path.GetFullPath(outputPath), File.Exists(outputPath),
                    options =>
                        options
                            .Seek(TimeSpan.Zero)
                            .WithDuration(duration < MinLength ? MinLength : duration)
                            .WithAudioCodec("libvorbis")
                            .WithAudioSamplingRate(16000)
                )
                .ProcessSynchronously();
        }
    }
}