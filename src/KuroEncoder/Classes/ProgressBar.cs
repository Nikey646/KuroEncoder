using System;
using System.Text;
using System.Threading;

namespace KuroEncoder.Classes
{
    public class ProgressBar : IDisposable, IProgress<Double>
    {
        private Int32 BlockCount = 10;
        private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private const String Animation = @"|/-\";

        private readonly Timer _timer;

        private Double _currentProgress = 0;
        private String _currentText = String.Empty;
        private Boolean _disposed = false;
        private Int32 _animationIndex = 0;
        private String _prefix;
        private Func<String> _suffixFunc = () => "";

        public ProgressBar(Int32 blockCount = -1) : this(String.Empty, blockCount)
        { }

        public ProgressBar(String prefix, Int32 blockCount = -1)
        {
            this._timer = new Timer(this.TimerHandler);
            this._prefix = prefix;

            this.BlockCount = blockCount;
            if (this.BlockCount < 0)
            {
                try
                {
                    this.BlockCount = Console.BufferWidth - prefix.Length - 10;
                }
                catch
                {
                    this.BlockCount = 25;
                }
            }

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                this.ResetTimer();
            }
        }

        public void SetSuffixFunc(Func<String> func)
        {
            this._suffixFunc = func ?? throw new ArgumentNullException(nameof(func));
        }

        public void Report(Double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref this._currentProgress, value);
        }

        private void TimerHandler(Object state)
        {
            lock (this._timer)
            {
                if (this._disposed) return;

                var suffix = this._suffixFunc();

                var blockCount = this.BlockCount - suffix.Length;

                var progressBlockCount = (Int32) (this._currentProgress * blockCount);
                var percent = (Int32) (this._currentProgress * 100);
                var text = String.Format("[{0}{1}] {2,3}% {3}",
                    new String('#', progressBlockCount), new String('-', blockCount - progressBlockCount),
                    percent,
                    Animation[this._animationIndex++ % Animation.Length]);
                this.UpdateText(this._prefix + text + suffix);

                this.ResetTimer();
            }
        }

        private void UpdateText(String text)
        {
            // Get length of common portion
            var commonPrefixLength = 0;
            var commonLength = Math.Min(this._currentText.Length, text.Length);
            while (commonPrefixLength < commonLength &&
                   text[commonPrefixLength] == this._currentText[commonPrefixLength])
            {
                commonPrefixLength++;
            }

            // Backtrack to the first differing character
            var outputBuilder = new StringBuilder();
            outputBuilder.Append('\b', this._currentText.Length - commonPrefixLength);

            // Output new suffix
            outputBuilder.Append(text.Substring(commonPrefixLength));

            // If the new text is shorter than the old one: delete overlapping characters
            var overlapCount = this._currentText.Length - text.Length;
            if (overlapCount > 0)
            {
                outputBuilder.Append(' ', overlapCount);
                outputBuilder.Append('\b', overlapCount);
            }

            Console.Write(outputBuilder);
            this._currentText = text;
        }

        private void ResetTimer()
        {
            this._timer.Change(this._animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public void Dispose()
        {
            lock (this._timer)
            {
                this._disposed = true;
                this.UpdateText(String.Empty);
            }

            this._timer.Dispose();
        }
    }
}
