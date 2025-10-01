using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Architecture_1.Common.AppConfigurations.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Globalization;

namespace Architecture_1.BusinessLogic.Helpers
{
    public class DateHelpers
    {
        public bool IsValidTime(string input)
        {
            // Sử dụng DateTime.TryParseExact để kiểm tra định dạng HH:mm
            return TimeOnly.TryParseExact(
                input,
                "HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _
            );
        }

        public bool IsValidDate(string input)
        {
            // Sử dụng DateTime.TryParseExact để kiểm tra định dạng yyyy-MM-dd
            return DateTime.TryParseExact(
                input,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _
            );
        }

        public bool IsTimeEarlier(string time1, string time2)
        {
            // Sử dụng TimeOnly.TryParse để chuyển đổi chuỗi thời gian thành kiểu TimeOnly
            if (TimeOnly.TryParseExact(time1, "HH:mm", null, System.Globalization.DateTimeStyles.None, out TimeOnly firstTime) &&
                TimeOnly.TryParseExact(time2, "HH:mm", null, System.Globalization.DateTimeStyles.None, out TimeOnly secondTime))
            {
                // So sánh hai thời gian
                return firstTime < secondTime;
            }
            else
            {
                // throw new HttpRequestException("Một hoặc cả hai chuỗi không phải là thời gian hợp lệ theo định dạng HH:mm.");
                throw new HttpRequestException("One or both strings are not valid times (required format: HH:mm), " + time1 + " - " + time2);
            }
        }

        public bool IsDateEarlier(string date1, string date2)
        {
            Console.WriteLine($"\n\n\ndate1: {date1}");
            Console.WriteLine($"\n\n\ndate2: {date2}");
            // Sử dụng DateTime.TryParse để chuyển đổi chuỗi ngày thành kiểu DateTime
            if (DateTime.TryParseExact(date1, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime firstDate) &&
                DateTime.TryParseExact(date2, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime secondDate))
            // if (DateTime.TryParse(date1, out DateTime firstDate) &&
            //     DateTime.TryParse(date2, out DateTime secondDate))
            {
                // So sánh hai ngày
                return firstDate < secondDate;
            }
            else
            {
                // throw new HttpRequestException("Một hoặc cả hai chuỗi không phải là ngày hợp lệ theo định dạng yyyy-MM-dd.");
                throw new HttpRequestException("One or both strings are not valid dates (required format: yyyy-MM-dd), " + date1 + " - " + date2);
            }
        }

        public bool IsDateTimeEarlier(string dateTime1, string dateTime2)
        {
            // Sử dụng DateTime.TryParse để chuyển đổi chuỗi ngày thành kiểu DateTime
            if (DateTime.TryParse(dateTime1, out DateTime firstDate) &&
                DateTime.TryParse(dateTime2, out DateTime secondDate))
            {
                // So sánh hai ngày
                return firstDate < secondDate;
            }
            else
            {
                // throw new HttpRequestException("Một hoặc cả hai chuỗi không phải là ngày hợp lệ theo định dạng yyyy-MM-dd.");
                throw new HttpRequestException("One or both strings are not valid dates (required format: yyyy-MM-dd), " + dateTime1 + " - " + dateTime2);
            }
        }

        public bool IsValidCloseScheduleAndOpenSchedule(string CloseScheduleDate, string OpenScheduleDate, string PrevCloseScheduleDate)
        {
            PrevCloseScheduleDate = PrevCloseScheduleDate == null || PrevCloseScheduleDate == "" ? null : PrevCloseScheduleDate;

            if (CloseScheduleDate != null && CloseScheduleDate != "")
            {
                if (IsValidDate(CloseScheduleDate) == false)
                {
                    // throw new HttpRequestException("Ngày đóng không hợp lệ");
                    throw new HttpRequestException("Invalid close date (required format: yyyy-MM-dd), " + CloseScheduleDate);
                }
                
                if (PrevCloseScheduleDate == null && IsDateEarlier(DateTime.Now.ToString("yyyy-MM-dd"), CloseScheduleDate) == false)
                {
                    // throw new HttpRequestException("Ngày đóng phải sau ngày hiện tại");
                    throw new HttpRequestException("Close date must be after the current date, " + CloseScheduleDate);
                }else if (PrevCloseScheduleDate != null && IsDateEarlier(PrevCloseScheduleDate, DateTime.Now.ToString("yyyy-MM-dd")) == false && IsDateEarlier(DateTime.Now.ToString("yyyy-MM-dd"), CloseScheduleDate) == false)
                {
                    // throw new HttpRequestException("Ngày đóng phải sau ngày hiện tại");
                    throw new HttpRequestException("Close date must be after the current date, " + CloseScheduleDate);
                }
            }
            if (OpenScheduleDate != null && OpenScheduleDate != "")
            {
                if (IsValidDate(OpenScheduleDate) == false)
                {
                    // throw new HttpRequestException("Ngày mở không hợp lệ");
                    throw new HttpRequestException("Invalid open date, " + OpenScheduleDate);
                }
                if (IsDateEarlier(DateTime.Now.ToString("yyyy-MM-dd"), OpenScheduleDate) == false)
                {
                    // throw new HttpRequestException("Ngày mở phải sau ngày hiện tại");
                    throw new HttpRequestException("Open date must be after the current date, " + OpenScheduleDate);
                }
            }
            if (OpenScheduleDate != "" && CloseScheduleDate == "")
            {
                // throw new HttpRequestException("Ngày đóng không được để trống");
                throw new HttpRequestException("Close date cannot be empty");
            }

            if (CloseScheduleDate != null && OpenScheduleDate != null && CloseScheduleDate != "" && OpenScheduleDate != "")
            {
                if (IsDateEarlier(CloseScheduleDate, OpenScheduleDate) == false)
                {
                    // throw new HttpRequestException("Ngày mở phải sau ngày đóng");
                    throw new HttpRequestException("Open date must be after close date, "+ CloseScheduleDate + " - " + OpenScheduleDate);
                }

            }
            return true;
        }


        public int GetDayOfWeekNumber(string dateString)
        {
            if (DateTime.TryParseExact(dateString, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            {
                return (int)date.DayOfWeek + 1; // Chủ Nhật (0) -> 1, Thứ Hai (1) -> 2, ..., Thứ Bảy (6) -> 7
            }
            else
            {
                // throw new HttpRequestException("Định dạng ngày không hợp lệ. Định dạng yêu cầu: yyyy-MM-dd");
                throw new HttpRequestException("Invalid date format (required format: yyyy-MM-dd), " + dateString);
            }
        }


        public bool IsTimeInRange(string time, string startTime, string endTime)
        {
            if (TimeOnly.TryParse(time, out TimeOnly t) &&
                TimeOnly.TryParse(startTime, out TimeOnly start) &&
                TimeOnly.TryParse(endTime, out TimeOnly end))
            {
                return t >= start && t <= end;
            }
            else
            {
                // throw new HttpRequestException("Định dạng thời gian không hợp lệ. Yêu cầu: HH:mm");
                throw new HttpRequestException("Invalid time format (required format: HH:mm), " + time + " - " + startTime + " - " + endTime);
            }
        }

        public bool IsDateInRange(string date, string startDate, string endDate)
        {
            if (DateTime.TryParse(date, out DateTime d) &&
                DateTime.TryParse(startDate, out DateTime start) &&
                DateTime.TryParse(endDate, out DateTime end))
            {
                return d >= start && d <= end;
            }
            else
            {
                // throw new HttpRequestException("Định dạng ngày không hợp lệ. Yêu cầu: yyyy-MM-dd");
                throw new HttpRequestException("Invalid date format (required format: yyyy-MM-dd), " + date + " - " + startDate + " - " + endDate);
            }
        }






    }
}
