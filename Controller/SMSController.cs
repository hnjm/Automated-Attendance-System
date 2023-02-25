﻿using Automated_Attendance_System.Entity;
using Automated_Attendance_System.Entity.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Automated_Attendance_System.Controller
{
    public class SMSController
    {
        public Entities _db = new Entities();
        public static SMSDTO SmsDtoList = new SMSDTO();
        private readonly object _dbLock = new object();

        public async Task<SMSDTO> GetSMSDTO(string enrollmentNumber)
        {
            await Task.Run(() =>
            {
                lock (_dbLock)
            {
                if (enrollmentNumber.StartsWith("1100"))
                {
                    string studentId = enrollmentNumber.Substring(enrollmentNumber.Length - 4);
                    SmsDtoList = _db.BSS_STUDENT.Where(w => w.STATUS == true && w.STUDENT_ID.EndsWith(studentId)).AsEnumerable().Select(s => new SMSDTO
                    {
                        Name = s.FIRST_NAME + " " + (string.IsNullOrEmpty(s.MIDDLE_NAME) ? " " : s.MIDDLE_NAME.Concat(" ")) + s.LAST_NAME,
                        EnrollmentNumber = enrollmentNumber,
                        PhoneNumber = s.MOBILE,
                        SMSCount = 0
                    }).FirstOrDefault();
                }
                else
                {
                    string empId = enrollmentNumber.Substring(enrollmentNumber.Length - 4);
                    SmsDtoList = _db.HR_EMPLOYEE.Where(w => w.STATUS == true && w.EMP_ID.EndsWith(empId)).AsEnumerable().Select(s => new SMSDTO
                    {
                        Name = s.EMP_FIRST_NAME + " " + (string.IsNullOrEmpty(s.EMP_MIDDLE_NAME) ? " " : s.EMP_MIDDLE_NAME.Concat(" ")) + s.EMP_LAST_NAME,
                        EnrollmentNumber = enrollmentNumber,
                        PhoneNumber = s.MOBILE,
                        SMSCount = 0
                    }).FirstOrDefault();
                }
            }
            });

            return SmsDtoList;
        }

        public Task SaveSMSDTOHistory(SMSDTO obj)
        {
            BSS_SMS_HISTORY _smsHistory = new BSS_SMS_HISTORY
            {
                SENT_TO = obj.EnrollmentNumber,
                ATTRIBUTE1 = obj.PhoneNumber,
                ATTRIBUTE2 = obj.SMSCount.ToString(),
                IS_SEND = (obj.SMSCount > 0),
            };

            var flag = _db.BSS_SMS_HISTORY.SingleInsertAsync(_smsHistory);

            Console.WriteLine($"SMS insertion flag: {flag} ");

            return flag;
        }
    }
}
