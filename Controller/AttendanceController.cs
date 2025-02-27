﻿using Automated_Attendance_System.Entity;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace Automated_Attendance_System.Controller
{
    public class AttendanceController
    {
        private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);



        public int GetAttendanceDeviceCount()
        {
            _semaphore.Wait(1);
            try
            {
                Entities _db = new Entities();
                return _db.BSS_ATTENDANCE_DEVICES.Where(w => w.Status == true).Count();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Fetching attendance device failed. Excetion Details: {ex.Message} AttendanceController.cs: 26.");
                return -1;
            }
            finally { _semaphore.Release(); }
        }

        public List<BSS_ATTENDANCE_DEVICES> GetAttendanceDevices()
        {
            _semaphore.Wait(1);
            try
            {
                Entities _db = new Entities();
                return _db.BSS_ATTENDANCE_DEVICES.Where(w => w.Status == true).ToList();
            }
            catch (Exception ex)
            {
                Log.Fatal($"Fetching Device List Failed. Excetion Details: {ex.Message} AttendanceController.cs: 41.");
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public async Task<int> RecordPreviousAttendance(List<BSS_ATTENDANCE_ZK> previousAttendances)
        {
            await _semaphore.WaitAsync(1);
            int flag = 0;
            try
            {
                Entities _db = new Entities();
                _db.BSS_ATTENDANCE_ZK.AddRange(previousAttendances);
                flag = await _db.SaveChangesAsync();
                //_db.Dispose();
                if (flag <= 0)
                {
                    Log.Error($"{string.Join(",", previousAttendances.Select(s=> new { s.Enrollment_Number, s.Machine_Number, s.Punch_Time }))} data not inserted.\n");
                    return flag;
                }
                Log.Information("Insertion of previous data success.\n");
                return flag;
            }
            catch (Exception ex)
            {
                Log.Fatal($"Bulk insertion into database failed. Excetion Details: {ex.Message} AttendanceController.cs: 57.\n");
                return flag;
            }
            finally
            {
                _semaphore.Release();
            }
        }


        //Inserts Attendance to Database
        public async Task<BSS_ATTENDANCE_ZK> RecordAttendance(int machineNumber, string enrollmentNumber, int verifyMethod, DateTime punchDate, TimeSpan punchTime, int workCode)
        {
            await _semaphore.WaitAsync(1);
            int enrollNumber = int.TryParse(enrollmentNumber, out int temp) ? temp : -1;

            BSS_ATTENDANCE_ZK attendanceEntity = new BSS_ATTENDANCE_ZK
            {
                Machine_Number = machineNumber,
                Enrollment_Number = enrollNumber,
                Verify_Method = verifyMethod,
                Punch_Date = punchDate,
                Punch_Time = punchTime,
                Work_Code = workCode,
                Sync_Status = false
            };
            try
            {
                Entities _db = new Entities();
                await _db.BSS_ATTENDANCE_ZK.SingleInsertAsync(attendanceEntity);
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Realtime Push : {ex.Message}\n");
                bool successFlag = RetryDBEntry(attendanceEntity).GetAwaiter().GetResult();
                if (!successFlag)
                {
                    return attendanceEntity;
                }
                return null;
            }
            finally { _semaphore.Release(); }
        }

        public async Task<bool> RetryDBEntry(BSS_ATTENDANCE_ZK errorEntry, int retryCount = 0)
        {
            await _semaphore.WaitAsync(1);
            bool result = false;

            try
            {
                Entities _db = new Entities();
                await _db.BSS_ATTENDANCE_ZK.SingleInsertAsync(errorEntry);
                result = true;
            }
            catch (Exception ex)
            {
                if (retryCount <= 1)
                {
                    retryCount += 1;
                    await RetryDBEntry(errorEntry, retryCount);
                    result = false;
                }
                else
                {
                    Console.WriteLine($"Insertion for {errorEntry.Enrollment_Number} failed in Retry Entry to DB. Exception: {ex.Message}\n ");
                    Log.Fatal($"Insertion for {errorEntry.Enrollment_Number} failed in Retry Entry to DB. Exception: {ex.Message}\n AttendanceController.cs: 119.\n");
                    result = false;
                }
            }
            finally { _semaphore.Release(); }

            return result;
            // The old code has higher time complexity as well as SaveChangesAsync is slower
            #region Old Code

            //BSS_ATTENDANCE_ZK temp = null;
            //foreach (BSS_ATTENDANCE_ZK att in errorList)
            //{
            //    temp = att;
            //    await _db.BSS_ATTENDANCE_ZK.SingleInsertAsync(att);
            //}
            //int saveFlag = await _db.SaveChangesAsync();
            //if (saveFlag > 0)
            //{
            //    errorList.RemoveAll(r => r == temp);
            //    temp = null;
            //}

            #endregion
        }
    }
}
