using System;
using System.Collections.Generic;

public class CustomAIData : ILoadableData
{
	public class Row
	{
		public int RepeatCooldown;

		public List<FunctionCall> Conditions = new List<FunctionCall>();

		public List<FunctionCall> Actions = new List<FunctionCall>();
	}

	public class FunctionCall
	{
		public string FunctionName;

		public List<string> Parameters = new List<string>();

		public bool Negated;

		public static FunctionCall Parse(string jsonString)
		{
			FunctionCall functionCall = new FunctionCall();
			int num = jsonString.IndexOf('(');
			int num2 = jsonString.IndexOf(')');
			if (num == -1 || num2 == -1)
			{
				throw new Exception();
			}
			functionCall.FunctionName = jsonString.Substring(0, num);
			string[] array = jsonString.Substring(num + 1, num2 - num - 1).Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text != string.Empty)
				{
					functionCall.Parameters.Add(text);
				}
			}
			return functionCall;
		}

		public string GetParam(int index)
		{
			if (Parameters.Count > index)
			{
				return Parameters[index];
			}
			throw new Exception("parameter " + (index + 1) + " not found");
		}

		public string GetParam(int index, string defaultValue)
		{
			if (Parameters.Count > index)
			{
				return Parameters[index];
			}
			return defaultValue;
		}
	}

	public const int NO_REPEAT = -99;

	private List<Row> mRows = new List<Row>();

	public string ID { get; private set; }

	public List<Row> Rows => mRows;

	public void Populate(Dictionary<string, object> dict)
	{
		ID = TFUtils.LoadString(dict, "ID", string.Empty);
		AddRow(dict);
	}

	public void AddRow(Dictionary<string, object> dict)
	{
		Row row = new Row();
		mRows.Add(row);
		row.RepeatCooldown = TFUtils.LoadInt(dict, "RepeatCooldown", -99);
		string[] array = TFUtils.LoadString(dict, "Conditions", string.Empty).Split(';');
		foreach (string text in array)
		{
			if (text == string.Empty)
			{
				continue;
			}
			try
			{
				bool negated = false;
				string text2 = text.Trim();
				if (text2.StartsWith("!"))
				{
					text2 = text2.Substring(1, text.Length - 1).Trim();
					negated = true;
				}
				FunctionCall functionCall = FunctionCall.Parse(text2);
				functionCall.Negated = negated;
				row.Conditions.Add(functionCall);
			}
			catch (Exception)
			{
			}
		}
		array = TFUtils.LoadString(dict, "Actions", string.Empty).Split(';');
		foreach (string text3 in array)
		{
			if (!(text3 == string.Empty))
			{
				try
				{
					FunctionCall item = FunctionCall.Parse(text3.Trim());
					row.Actions.Add(item);
				}
				catch (Exception)
				{
				}
			}
		}
	}
}
