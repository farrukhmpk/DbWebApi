﻿// Copyright (c) 2015 Abel Cheng <abelcys@gmail.com> and other contributors.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Repository:	https://github.com/DataBooster/DbWebApi

using System;
using System.Threading;
using DbParallel.DataAccess;
using DataBooster.DbWebApi.DataAccess;

namespace DataBooster.DbWebApi
{
	public static partial class WebApiExtensions
	{
		public static int DetectSpChanges(int elapsedMinutes)
		{
			int cntExpired = -1;
			string detectDdlChangesProc = ConfigHelper.DetectDdlChangesProc;

			if (string.IsNullOrEmpty(detectDdlChangesProc) || elapsedMinutes < 1)
				SwitchDerivedParametersCache(false);
			else
			{
				SwitchDerivedParametersCache(true);

				using (DbContext dbContext = new DbContext())
				{
					cntExpired = dbContext.InvalidateAlteredSpFromCache(detectDdlChangesProc, TimeSpan.FromMinutes(elapsedMinutes));
				}

				LastDetectSpChangesTime = DateTime.Now;
			}

			return cntExpired;
		}

		private static long _LastDetectSpChangesTicks;
		private static DateTime LastDetectSpChangesTime
		{
			get { return new DateTime(Interlocked.Read(ref _LastDetectSpChangesTicks)); }
			set { Interlocked.Exchange(ref _LastDetectSpChangesTicks, value.Ticks); }
		}

		private static bool _DerivedParametersCacheInPeriodicDetection = false;
		internal static bool DerivedParametersCacheInPeriodicDetection
		{
			get { return _DerivedParametersCacheInPeriodicDetection; }
		}

		private static void SwitchDerivedParametersCache(bool periodicDetection)
		{
			if (periodicDetection != _DerivedParametersCacheInPeriodicDetection)
			{
				_DerivedParametersCacheInPeriodicDetection = periodicDetection;

				DerivedParametersCache.ExpireInterval = periodicDetection ?
					DbWebApiOptions.DetectDdlChangesContract.CacheExpireIntervalWithDetection :
					DbWebApiOptions.DetectDdlChangesContract.CacheExpireIntervalWithoutDetection;
			}
		}

		internal static bool SelfRecoverDerivedParametersCache()
		{
			if (_DerivedParametersCacheInPeriodicDetection)
			{
				TimeSpan thresholdInterval = DbWebApiOptions.DetectDdlChangesContract.CacheExpireIntervalWithoutDetection;

				if (thresholdInterval > TimeSpan.Zero && DateTime.Now - LastDetectSpChangesTime > thresholdInterval)
				{
					SwitchDerivedParametersCache(false);
					return true;
				}
			}

			return false;
		}
	}
}
