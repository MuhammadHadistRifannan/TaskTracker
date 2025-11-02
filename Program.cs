using System;
using System.ComponentModel;
using System.Data;
using System.IO.Enumeration;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.VisualBasic;
using Newtonsoft.Json;


namespace TaskTracker
{
    enum StatusTask
    {
        ToDo = 1,
        InProgress = 2,
        Done = 3
    }

    struct TaskStructure
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public StatusTask status { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
    }

    static class Identifier
    {
        public static int GetId()
        {
            return new Random(DateTime.Now.Microsecond).Next(0, 999);
        }
    }
    
    static class Extension
    {
        /// <summary>
        /// Return 0 if the string is null or zero-length
        /// </summary>
        /// <param name="_field"></param>
        /// <returns></returns>
        public static int TryGetLengthAt(this string[] _field, int index)
        {
            try
            {
                return _field[index].Length;
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        /// <summary>
        /// Return true if the tasks is not empty , otherwise
        /// </summary>
        /// <param name="tasks"></param>
        /// <returns></returns>
        public static bool IsNotEmpty(this List<TaskStructure> tasks)
        {
            if (tasks.Count == 0)
            {
                return false;
            }
            return true;
        }
    }

    public class Program
    {
        readonly static string filePath = FileSystem.CurDir() + "/task.json";
        public static void Main(string[] args)
        {
            if (args.TryGetLengthAt(0) == 0)
            {
                Console.Write("use the tracker help to find the appropriate command");
                return;
            }
            var command = args[0];

            switch (command)
            {
                case "add":
                    try
                    {
                        var opt = args[1];

                        //Check validity 
                        CheckValidCmd(opt, 1);
                        //Add specified task
                        Console.Write($"description : ");
                        string description = Console.ReadLine()!;
                        Console.Write($"status (1.ToDo 2.Progress 3.Done) : ");
                        int status = 0;
                        if (int.TryParse(Console.ReadLine()! , out status) && (status > 0 && status <= 3))
                        {
                            //Create Task 
                            var task = new TaskStructure
                            {
                                id = Identifier.GetId(),
                                name = opt,
                                description = description,
                                status = (StatusTask)status,
                                created_at = DateTime.Now,
                                updated_at = DateTime.Now
                            };

                            Add(task);
                        }
                        else Console.WriteLine("Task status must be number <1 - 3>");
                    }catch (Exception e)
                    {
                        if (e.GetType() == typeof(IndexOutOfRangeException)) Console.WriteLine("Wrong Command\nu should use tracker add \"<task_name>\"");
                        else Console.WriteLine(e.Message);
                    }
                    break;
                case "update":
                    if (args.TryGetLengthAt(1) == 0) { Console.WriteLine("do you mean tracker update \"<id_task>\""); return; }
                    try
                    {
                        CheckValidCmd(args[1], 2);
                        int idTask = int.Parse(args[1]);
                        Update(idTask);
                    }catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    break;
                case "mark-in-done":
                    if (args.TryGetLengthAt(1) == 0) { Console.WriteLine("use mark-in-done <Task_Id>"); return; }
                    int id;
                    if (int.TryParse(args[1] , out id))
                    {
                        UpdateStatus(id, 3);
                    }else
                    {
                        Console.Write("use digit instead");
                    }

                    break;
                case "mark-in-progress":
                    if (args.TryGetLengthAt(1) == 0) { Console.WriteLine("use mark-in-progress <Task_Id>"); return; }
                    int idSt;
                    if (int.TryParse(args[1], out idSt))
                    {
                        UpdateStatus(idSt, 2);
                    }
                    else
                    {
                        Console.Write("use digit instead");
                    }
                    break;
                case "list":
                    if (args.TryGetLengthAt(1) != 0)
                    {
                        switch (args[1])
                        {
                            case "todo":
                                ListTask((StatusTask)1);
                                break;
                            case "done":
                                ListTask((StatusTask)3);
                                break;
                            case "in-progress":
                                ListTask((StatusTask)2);
                                break;
                            default:
                                Console.WriteLine("use done , todo , in-progress instead");
                                break;
                        }
                    }else
                    {
                        ListTask();
                    }
                    break;
                case "delete":
                    if (args.TryGetLengthAt(1) == 0){ Console.Write("u must specified task id , tracker delete <task_id>"); return; }
                    int idDel;
                    if (int.TryParse(args[1], out idDel))
                    {
                        Delete(idDel);
                    }
                    else
                    {
                        Console.WriteLine("Please use digit");
                    }

                    break;
                case "help":
                    Console.WriteLine("use tracker <COMMAND> <OPTION> \ntracker add <task_name> \ntracker update <task_id>\ntracker delete <task_id>\ntracker mark-in-progress <task_id>\ntracker mark-in-done <task_id>\ntracker list");
                    break;
                default:
                    Console.WriteLine("Command not found !! , use tracker -h (to find command)");
                    break;
            }
        }

        /// <summary>
        /// Throwing an exception if the command used is not valid
        /// </summary>
        /// <param name="opt"></param>
        /// <param name="idCommand"></param>
        /// <exception cref="Exception"></exception>
        static void CheckValidCmd(string opt, int idCommand)
        {
            switch (idCommand)
            {
                case 1:
                    if (opt.Length < 2) throw new Exception("Wrong Command\nu should use tracker add \"<task_name>\" or task name must be greater than 2");
                    break;
                case 2:
                    bool isNumber = char.IsDigit(opt[0]);
                    if (opt.Length > 3 || !isNumber) throw new Exception("Wrong Command\nu should use tracker update \"<id_task>\" <new_task_name>");
                    break;
            }
        }
        static bool isFileExist(string filepath)
        {
            return File.Exists(filepath);
        }
        static List<TaskStructure> JsonFile(string filepath)
        {
            var content = File.ReadAllText(filepath);
            return JsonConvert.DeserializeObject<List<TaskStructure>>(content);
        }
        private static void Add(TaskStructure task)
        {
            if (!isFileExist(filePath))
            {
                var content = new List<TaskStructure>();
                content.Add(task);
                var json = JsonConvert.SerializeObject(content);
                File.WriteAllText(filePath, json);
            }
            else
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    List<TaskStructure> tasks = JsonConvert.DeserializeObject<List<TaskStructure>>(content);
                    tasks.Add(task);
                    var json = JsonConvert.SerializeObject(tasks);
                    File.WriteAllText(filePath, json);
                }catch (NullReferenceException e)
                {
                    List<TaskStructure> newTask = new List<TaskStructure>();
                    newTask.Add(task);
                    var content = JsonConvert.SerializeObject(newTask);
                    File.WriteAllText(filePath, content);
                }
            }
        }
        private static void Delete(int taskId)
        {
            if (!isFileExist(filePath)) return;
            try
            {
                var content = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<List<TaskStructure>>(content);
                var getTask = list!.Where(e => e.id == taskId).FirstOrDefault();
                if (list.Remove(getTask))
                {
                    var newList = JsonConvert.SerializeObject(list);

                    File.WriteAllText(filePath, newList);
                    Console.WriteLine($"Task {getTask.name} successfully deleted");
                }else
                {
                    Console
                    .WriteLine($"Id Task {taskId} not found");
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("There are no task running ");
            }
        }
        private static void ListTask(StatusTask statusTask)
        {
            if (!isFileExist(filePath))return;
            var json = JsonFile(filePath);
            Console.WriteLine("=============================================");
                foreach (var item in json)
                {
                    if (item.status == statusTask)
                    {
                        
                        Console.WriteLine($" ID Task : {item.id}\n Task Name : {item.name}\n Description : {item.description}\n Status : {item.status}");
                        Console.WriteLine("-----------------------------");
                    }
                }
                Console.WriteLine("==============================================");
        }
        private static void ListTask()
        {
            if (!isFileExist(filePath)) return;
            try
            {
                var content = File.ReadAllText(filePath);
                var list = JsonConvert.DeserializeObject<List<TaskStructure>>(content);
                var tasks = list.Select(e => new { e.id, e.name, e.description, e.status });
                Console.WriteLine("=============================================");
                foreach (var item in tasks)
                {

                    Console.WriteLine($" ID Task : {item.id}\n Task Name : {item.name}\n Description : {item.description}\n Status : {item.status}");
                    Console.WriteLine("-----------------------------");
                }
                Console.WriteLine("==============================================");
            } catch (NullReferenceException e)
            {
                Console.WriteLine(e.Message);
            }
        }
        private static void Update(int taskId)
        {
            if (!isFileExist(filePath)) return;
            var content = File.ReadAllText(filePath);
            var json = JsonConvert.DeserializeObject<List<TaskStructure>>(content);
            var isAny = json.Where(e => e.id == taskId).ToList().IsNotEmpty();
            if (!isAny)
            {
                Console.WriteLine($"There's no Task Id {taskId} in list");
                return;
            }
            var getTask = json.Where(e => e.id == taskId).FirstOrDefault();

            Console.WriteLine($"1.Name      :{getTask.name}\n2.Status    :{getTask.status}");
            Console.WriteLine("<1 or 2> to update name or status \n>>");
            string updateTaskinput = Console.ReadLine();
            int updateId;
            if (int.TryParse(updateTaskinput, out updateId))
            {
                switch (updateId)
                {
                    case 1:
                        Console.WriteLine("New Task Name : ");
                        string newName = Console.ReadLine();
                        var oldName = getTask;
                        getTask.name = newName;
                        //Remove the old value 
                        json.Remove(oldName);
                        //Changing to the newest
                        json.Add(getTask);
                        var updatedName = JsonConvert.SerializeObject(json);
                        File.WriteAllText(filePath, updatedName);
                        Console.WriteLine("SuccessFully Update");
                        break;
                    case 2:
                        Console.Write("New Task Status : ");
                        string status = Console.ReadLine();

                        int st;
                        if (int.TryParse(status, out st))
                        {
                            var oldStatus = getTask;
                            getTask.status = (StatusTask)st;
                            //Remove the old value 
                            json.Remove(oldStatus);
                            //Changing to the newest
                            json.Add(getTask);
                            var updatedStatus = JsonConvert.SerializeObject(json);
                            File.WriteAllText(filePath, updatedStatus);
                            Console.WriteLine("SuccessFully Update");
                        }
                        else
                        {
                            Console.WriteLine("use digitt");
                        }
                        break;
                }
            }
            else
            {
                Console.WriteLine("please use digit");
            }
        }
        private static void UpdateStatus(int taskId, int newStatus)
        {
            if (!isFileExist(filePath)) return;
            var json = JsonFile(filePath);
            var getTask = json.Where(e => e.id == taskId).FirstOrDefault();
            var oldVal = getTask;
            getTask.status = (StatusTask)newStatus;
            json.Remove(oldVal);
            json.Add(getTask);
            var newJson = JsonConvert.SerializeObject(json);
            File.WriteAllText(filePath, newJson);
            Console.WriteLine("Successfully update status");
        }
    }
}