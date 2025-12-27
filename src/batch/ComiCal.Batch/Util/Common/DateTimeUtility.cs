using ComiCal.Batch.Models;
using ComiCal.Shared.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ComiCal.Batch.Util.Common
{
    public static class DateTimeUtility
    {

        public static (DateTime value, ScheduleStatus status) JpDateToDateTimeType(string value)
        {
            var canParse = DateTime.TryParseExact(value, "yyyy年MM月dd日", null, DateTimeStyles.None, out var res);
            if (canParse) return (res, ScheduleStatus.Confirm);

            var canParse2 = DateTime.TryParseExact(value, "yyyy年MM月dd日頃", null, DateTimeStyles.None, out var res2);
            if (canParse2) return (res2, ScheduleStatus.UntilDay);

            var canParse3 = DateTime.TryParseExact(value, "yyyy年MM月", null, DateTimeStyles.None, out var res3);
            if (canParse3) return (res3, ScheduleStatus.UntilMonth);

            var canParse4 = DateTime.TryParseExact(value, "yyyy年MM月上旬", null, DateTimeStyles.None, out var res4);
            if (canParse4) return (res4, ScheduleStatus.UntilMonth);

            var canParse5 = DateTime.TryParseExact(value, "yyyy年MM月中旬", null, DateTimeStyles.None, out var res5);
            if (canParse5) return (res5, ScheduleStatus.UntilMonth);

            var canParse6 = DateTime.TryParseExact(value, "yyyy年MM月下旬", null, DateTimeStyles.None, out var res6);
            if (canParse6) return (res6, ScheduleStatus.UntilMonth);

            var canParse7 = DateTime.TryParseExact(value, "yyyy年", null, DateTimeStyles.None, out var res7);
            if (canParse7) return (res7, ScheduleStatus.UntilYear);

            var canParse8 = DateTime.TryParseExact(value, "yyyy年頃", null, DateTimeStyles.None, out var res8);
            if (canParse8) return (res8, ScheduleStatus.UntilYear);

            return (new DateTime(1,1,1,0,0,0,0), ScheduleStatus.Undecided);
        }
    }
}
