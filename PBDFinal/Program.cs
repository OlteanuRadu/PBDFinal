using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PBDFinal
{
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<int, Action> _strategyDictionary = new Dictionary<int, Action>();
            _strategyDictionary.Add(1, () =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Processing ...");

                new TextAnalizer().ReadDataFromDisk().ForEach(_ => Console.WriteLine($"{_}"));
                Console.WriteLine($"Done ....");
            });
            _strategyDictionary.Add(2, () =>
            {
                using (var context = new AsdfEntities1())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Saving to Database ....");

                    new TextAnalizer().ReadDataFromDisk().ForEach(_ =>
            {
                var indexSecondCapitalLetter = _.Substring(0, _.IndexOf(" ", StringComparison.CurrentCulture))
                                                .IndexOf(_.Substring(0, _.IndexOf(" ", StringComparison.CurrentCulture))
                                                .ToArray().Where(a => char.IsUpper(a))
                                                .Skip(1)
                                                .FirstOrDefault());

                var t = _.Substring(_.IndexOf(" ", StringComparison.CurrentCulture));
                var finalDate = t.Insert(t.IndexOf("/", StringComparison.CurrentCulture) + 7, " ");

                DateTimeOffset studentAttendanceDate = Convert
                                             .ToDateTime(finalDate);

                if (indexSecondCapitalLetter == -1)
                {
                    var firstNamee = _.Substring(0, _.IndexOf(" ", StringComparison.CurrentCulture));
                    var lastNamee = _.Substring(0, _.IndexOf(" ", StringComparison.CurrentCulture));
                    SaveToEfDb(context, new Studentt { FirstName = firstNamee, LastName = lastNamee }, studentAttendanceDate);
                }
                else
                {
                    var firstName = _.Substring(0, _.IndexOf(" ", StringComparison.CurrentCulture)).Substring(0, indexSecondCapitalLetter);
                    var lastName = _.Substring(0, _.IndexOf(" ", StringComparison.CurrentCulture)).Substring(indexSecondCapitalLetter);
                    SaveToEfDb(context, new Studentt { FirstName = firstName, LastName = lastName }, studentAttendanceDate);
                }

                context.SaveChanges();
                Console.WriteLine($"Saved succesfully ... ");
            });
                }

            });
            _strategyDictionary.Add(3, () =>
            {
                using (var context = new AsdfEntities1())
                {
                    context.StudentAttendance.RemoveRange(context.StudentAttendance);
                    context.Studentt.RemoveRange(context.Studentt);
                    context.SaveChanges();
                    Console.WriteLine($"Succesfully deleted ...");
                }
            });
            _strategyDictionary.Add(4, () =>
            {

                using (var context = new AsdfEntities1())
                {
                    context
                            .Studentt.Join(context.StudentAttendance, student => student.Id, sa => sa.IdStudent, (s, sa) => new
                            {
                                Studentt = s,
                                StudentAttendance = sa
                            }).GroupBy(_ =>
                                            new
                                            {
                                                _.Studentt.FirstName,
                                                _.Studentt.LastName
                                            })
                      .OrderBy(g => g.Count())
                      .Select(_ => new
                      {
                          _.Key.FirstName,
                          _.Key.LastName,
                          Attendance = _.Count()
                      })
                      .ToList()
                      .ForEach(_ => Console.WriteLine($"{_.FirstName} {_.LastName} has {_.Attendance} attendances"));
                }
            });

            ShowMenu();

            while (true)
            {

                var line = Console.ReadLine();
                int result;
                int.TryParse(line, out result);

                if (result > 0)
                {

                    _strategyDictionary.ContainsKey(result).IsTrue(() => _strategyDictionary[result](), () =>
                    {
                        Console.WriteLine("Wrong menu item selected ....");
                    });
                }
                else
                {

                    if (line.ToLower() == "clear" || line.ToLower().Contains("clear"))
                    {
                        Console.Clear();
                        ShowMenu();
                    }
                    else
                    {
                        Console.WriteLine("Wrong menu item selected ....");
                    }
                }
            }
        }

        static void ShowMenu()
        {
            Console.ResetColor();
            Console.WriteLine("1. Display student attendance extracted from the logs ...");
            Console.WriteLine("2. Save to the database ... ");
            Console.WriteLine("3. Clear datebase ... ");
            Console.WriteLine("4. Display attendancee ...");

        }

        static void SaveToEfDb(AsdfEntities1 context, Studentt entity, DateTimeOffset studentAttendanceDate)
        {
            context.StudentAttendance.Add(new StudentAttendance
            {
                Studentt = entity,
                Date = studentAttendanceDate
            });
        }
    }


    public class TextAnalizer
    {
        public TextAnalizer()
        {

        }
        public IEnumerable<string> ReadDataFromDisk()
        {
            try
            {
                using (var strRd = new StreamReader(@"C:\Users\radu_olteanu\Documents\fzs-2016-10-03.log"))
                {
                    string line = strRd.ReadToEnd();

                    var lookUpCWD = line
                                               .Split(' ')
                                               .Skip(line.IndexOf("CWD", StringComparison.Ordinal));

                    var lookUpMKD = line
                                     .Split(' ')
                                     .Skip(line.IndexOf("CWD", StringComparison.Ordinal))
                                     .Select((b, i) => b == "MKD" ? i : -1)
                                     .Where(_ => _ != -1);

                    List<string> items = new List<string>();
                    List<string> finalList = new List<string>();

                    foreach (var item in lookUpMKD)
                    {
                        items.Add($@"{lookUpCWD.ToList()[item + 1]} 
                                     {lookUpCWD.ToList()[item + 2]} 
                                     {lookUpCWD.ToList()[item + 3]}
                                     {lookUpCWD.ToList()[item + 4]}");
                    }

                    items.ForEach(_ =>
                    {
                        _ = _.Replace("\r", "").Replace("\n", "").Replace(" ", string.Empty);
                        Regex regex = new Regex(@"\(([^\}]+)\)");
                        _ = regex.Replace(_, " ");
                        if (!_.ToLower().Contains("lab") && !_.ToLower().Contains("essential") && !_.ToLower().Contains("syntax") && !_.ToLower().Contains("what"))
                            finalList.Add(_);
                    });

                    return finalList;
                }
            }

            catch (Exception e)
            {
                throw e;
            }
        }
    }

    public static class ExtensionUtils
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void IsTrue(this bool source, Action action1, Action action2 = null)
        {
            if (source == true) action1();
            else
            {
                action2();
            }
        }
    }

}
