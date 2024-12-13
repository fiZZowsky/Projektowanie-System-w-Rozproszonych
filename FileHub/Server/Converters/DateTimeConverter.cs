using Google.Protobuf.WellKnownTypes;

namespace Server.Converters
{
    public static class DateTimeConverter
    {
        public static Google.Protobuf.WellKnownTypes.Timestamp ConvertToTimestamp(DateTime dateTime)
        {
            return new Google.Protobuf.WellKnownTypes.Timestamp
            {
                Seconds = (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds,
                Nanos = dateTime.Millisecond * 1000000
            };
        }

        public static DateTime ConvertToDateTime(Google.Protobuf.WellKnownTypes.Timestamp timestamp)
        {
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(timestamp.Seconds)
                .AddMilliseconds(timestamp.Nanos / 1000000)
                .DateTime;

            return dateTime;
        }

        public static DateTime ConvertToDateTime(string input)
        {
            string format = "yyyyMMdd-HHmmss";

            DateTime dateTime = DateTime.ParseExact(input, format, null);

            return dateTime;
        }
    }
}
