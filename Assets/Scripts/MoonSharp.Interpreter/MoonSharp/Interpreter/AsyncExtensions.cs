using System;
using System.IO;
using System.Threading.Tasks;
using MoonSharp.Interpreter.REPL;

namespace MoonSharp.Interpreter
{
	public static class AsyncExtensions
	{
		private static Task<T> ExecAsync<T>(Func<T> func)
		{
			return Task.Factory.StartNew(func);
		}

		private static Task ExecAsyncVoid(Action func)
		{
			return Task.Factory.StartNew(func);
		}

		public static Task<DynValue> CallAsync(this Closure function)
		{
			return ExecAsync(() => function.Call());
		}

		public static Task<DynValue> CallAsync(this Closure function, params object[] args)
		{
			return ExecAsync(() => function.Call(args));
		}

		public static Task<DynValue> CallAsync(this Closure function, params DynValue[] args)
		{
			return ExecAsync(() => function.Call(args));
		}

		public static Task<DynValue> DoStringAsync(this Script script, string code, Table globalContext = null, string codeFriendlyName = null)
		{
			return ExecAsync(() => script.DoString(code, globalContext, codeFriendlyName));
		}

		public static Task<DynValue> DoStreamAsync(this Script script, Stream stream, Table globalContext = null, string codeFriendlyName = null)
		{
			return ExecAsync(() => script.DoStream(stream, globalContext, codeFriendlyName));
		}

		public static Task<DynValue> DoFileAsync(this Script script, string filename, Table globalContext = null, string codeFriendlyName = null)
		{
			return ExecAsync(() => script.DoFile(filename, globalContext, codeFriendlyName));
		}

		public static Task<DynValue> LoadFunctionAsync(this Script script, string code, Table globalTable = null, string funcFriendlyName = null)
		{
			return ExecAsync(() => script.LoadFunction(code, globalTable, funcFriendlyName));
		}

		public static Task<DynValue> LoadStringAsync(this Script script, string code, Table globalTable = null, string codeFriendlyName = null)
		{
			return ExecAsync(() => script.LoadString(code, globalTable, codeFriendlyName));
		}

		public static Task<DynValue> LoadStreamAsync(this Script script, Stream stream, Table globalTable = null, string codeFriendlyName = null)
		{
			return ExecAsync(() => script.LoadStream(stream, globalTable, codeFriendlyName));
		}

		public static Task DumpAsync(this Script script, DynValue function, Stream stream)
		{
			return ExecAsyncVoid(delegate
			{
				script.Dump(function, stream);
			});
		}

		public static Task<DynValue> LoadFileAsync(this Script script, string filename, Table globalContext = null, string friendlyFilename = null)
		{
			return ExecAsync(() => script.LoadFile(filename, globalContext, friendlyFilename));
		}

		public static Task<DynValue> CallAsync(this Script script, DynValue function)
		{
			return ExecAsync(() => script.Call(function));
		}

		public static Task<DynValue> CallAsync(this Script script, DynValue function, params DynValue[] args)
		{
			return ExecAsync(() => script.Call(function, args));
		}

		public static Task<DynValue> CallAsync(this Script script, DynValue function, params object[] args)
		{
			return ExecAsync(() => script.Call(function, args));
		}

		public static Task<DynValue> CallAsync(this Script script, object function)
		{
			return ExecAsync(() => script.Call(function));
		}

		public static Task<DynValue> CallAsync(this Script script, object function, params object[] args)
		{
			return ExecAsync(() => script.Call(function, args));
		}

		public static Task<DynamicExpression> CreateDynamicExpressionAsync(this Script script, string code)
		{
			return ExecAsync(() => script.CreateDynamicExpression(code));
		}

		public static Task<DynValue> EvaluateAsync(this ReplInterpreter interpreter, string input)
		{
			return ExecAsync(() => interpreter.Evaluate(input));
		}

		public static Task<DynValue> ResumeAsync(this Coroutine cor, params DynValue[] args)
		{
			return ExecAsync(() => cor.Resume(args));
		}

		public static Task<DynValue> ResumeAsync(this Coroutine cor, ScriptExecutionContext context, params DynValue[] args)
		{
			return ExecAsync(() => cor.Resume(context, args));
		}

		public static Task<DynValue> ResumeAsync(this Coroutine cor)
		{
			return ExecAsync(() => cor.Resume());
		}

		public static Task<DynValue> ResumeAsync(this Coroutine cor, ScriptExecutionContext context)
		{
			return ExecAsync(() => cor.Resume(context));
		}

		public static Task<DynValue> ResumeAsync(this Coroutine cor, params object[] args)
		{
			return ExecAsync(() => cor.Resume(args));
		}

		public static Task<DynValue> ResumeAsync(this Coroutine cor, ScriptExecutionContext context, params object[] args)
		{
			return ExecAsync(() => cor.Resume(context, args));
		}
	}
}
