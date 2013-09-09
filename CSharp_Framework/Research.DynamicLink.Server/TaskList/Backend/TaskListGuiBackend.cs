using Research.DynamicLink.Server.TaskList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Research.DynamicLink.Server
{
  public class TaskListGuiBackend
  {

    public static object ShowTaskList()
    {
      var Model = new ShowTaskListViewModel();
      Model.Name = "Kevin Küpper";
      Model.Tasks = new List<Task>();
      Model.Tasks.Add(new Task() { TaskName = "TSS Projekt planen", Priority = "Hoch", Description = "Siehe Programmierer-Aufgaben-Verwaltung" });
      Model.Tasks.Add(new Task() { TaskName = "Kuchen backen", Priority = "Normal", Description = "Wasser, Mehr und Zucker..." });
      return Model;
    }

  }
}
