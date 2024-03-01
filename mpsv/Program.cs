using System.IO.Pipes;

#nullable disable
namespace mpsv;

internal class Program
{

	private static NamedPipeServerStream _s;
	private static StreamWriter          _sw;
	private static StreamReader          _sr;
	private static string                _pipeName;

	public const   string                  MPSV_NAME = "mpsv";
	private static CancellationTokenSource _cts      = new CancellationTokenSource();
	static         Mutex                   mutex     = new Mutex(true, "MPSV");

	public static async Task<int> Main(string[] args)
	{
		if (mutex.WaitOne(TimeSpan.Zero)) {
			_s = null;

			_sw = StreamWriter.Null;
			_sr = StreamReader.Null;

			_pipeName = MPSV_NAME;

#if DEBUG

#endif

			switch (args) {
				case { Length: > 1 }:
					_pipeName = MPSV_NAME;
					break;

				default:
					break;
			}

			_pipeName = $@"\\.\pipe\{_pipeName}";
			Console.WriteLine($"{_pipeName}");

			Console.CancelKeyPress += (sender, eventArgs) =>
			{
				_cts.Cancel();

				Close();

			};

			try {
				_s = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message,
				                               PipeOptions.Asynchronous)
					{ };

				/*s=NamedPipeServerStreamAcl.Create(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message,
											   PipeOptions.Asynchronous,
											   additionalAccessRights: PipeAccessRights.FullControl, inBufferSize: 8192,
											   outBufferSize: 8192, pipeSecurity: null,
											   inheritability: HandleInheritability.None)*/
				;

				Console.WriteLine($"{_s}");

				var r = _s.BeginWaitForConnection(Callback, null);

				r.AsyncWaitHandle.WaitOne();

				_sw = new StreamWriter(_s) { AutoFlush = false };
				_sr = new StreamReader(_s) { };

				await _sw.WriteLineAsync("Connected");

				while (!_cts.IsCancellationRequested) {
					var l = await _sr.ReadLineAsync();
					Console.WriteLine($"{l}");

				}
			}
			catch (TaskCanceledException x) {
				Console.Error.WriteLine($"{x}");

			}
			catch (IOException x) {
				Console.Error.WriteLine($"{x}");
			}
			finally {
				Close();
				mutex.ReleaseMutex();

			}

		}
		else {
			Console.WriteLine("Already running");
			Console.ReadKey();
		}

		return 0;
	}

	private static void Callback(IAsyncResult ar)
	{
		_s.EndWaitForConnection(ar);

	}

	private static void Close()
	{
		_s?.Dispose();

		// _s?.Disconnect();

		_sw.Dispose();
		_sr.Dispose();
		Console.WriteLine("Disconnected");

	}

}