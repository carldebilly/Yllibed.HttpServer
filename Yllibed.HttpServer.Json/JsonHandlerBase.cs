﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Yllibed.HttpServer.Handlers;
using Newtonsoft.Json;
using Yllibed.Framework.Logging;

namespace Yllibed.HttpServer.Json
{
	public abstract class JsonHandlerBase<TResult> : IHttpHandler
		where TResult : class
	{
		private readonly string _method;
		private readonly string _path;

		protected JsonHandlerBase(string method, string path)
		{
			_method = method;
			_path = path;

			if (!_path.StartsWith("/"))
			{
				_path = "/" + _path;
			}
		}

		public async Task HandleRequest(CancellationToken ct, IHttpServerRequest request, string relativePath)
		{
			if (!request.Method.Equals(_method, StringComparison.OrdinalIgnoreCase))
			{
				return; // wrong method
			}

			var queryParts = relativePath.Split('?').ToList();

			if (queryParts[0].Equals(_path, StringComparison.OrdinalIgnoreCase))
			{
				try
				{
					var queryParameters = ParseQueryParameters(queryParts.Skip(1).FirstOrDefault());
					(var result, var statusCode) = await ProcessRequest(ct, relativePath, queryParameters);

					var json = JsonConvert.SerializeObject(result, Formatting.Indented);

					request.SetResponse("application/json", json, statusCode);
				}
				catch (Exception ex)
				{
					this.Log().Error($"Error processing request for path {relativePath}, error={ex.Message}");
					request.SetResponse("text/plain", "Error processing request", 500, "ERROR");
				}
			}
		}

		private IDictionary<string, string[]> ParseQueryParameters(string queryPart)
		{
			if (string.IsNullOrWhiteSpace(queryPart))
			{
				return ImmutableDictionary<string, string[]>.Empty;
			}

			var result = queryPart
				.Split('&')
				.Select(v => v.Split('='))
				.Where(v => v.Length == 2)
				.GroupBy(v => v[0], v => Uri.UnescapeDataString(v[1]))
				.Select(x => new KeyValuePair<string, string[]>(x.Key, x.ToArray()))
				.ToImmutableDictionary();

			return result;
		}

		protected abstract Task<(TResult result, ushort statusCode)> ProcessRequest(CancellationToken ct, string relativePath, IDictionary<string, string[]> queryParameters);
	}
}