using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelForEach
{
    class Program
    {
        static void Main()
   {            
      try {
         TraverseTreeParallelForEach(@"/home/sasha/wargamming/1", (f) =>
         {
            // Exceptions are no-ops.
            try {
               // Do nothing with the data except read it.
               // Ничего не делаю с данными, кроме чтения.
               //byte[] data = File.ReadAllBytes(f);
            }
            catch (FileNotFoundException) {}
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
            catch (SecurityException) {}
            // Display the filename.
            //Console.WriteLine(f);
         });
      }
      catch (ArgumentException) {
         Console.WriteLine(@"The directory 'C:\Program Files' does not exist.");
      }   

      // Keep the console window open.
      Console.ReadKey();
   }

   public static void TraverseTreeParallelForEach(string root, Action<string> action)
   {
      //Count of files traversed and timer for diagnostic output
      // Количество пройденных файлов и таймер для диагностического вывода
      int fileCount = 0;
      var sw = Stopwatch.StartNew();

      // Determine whether to parallelize file processing on each folder based on processor count.
      // Определите, следует ли распараллеливать обработку файлов в каждой папке на основе количества процессоров.
      int procCount = 4;
      Console.WriteLine("ProcessorCount = {0}; procCount = {1} ",System.Environment.ProcessorCount, procCount);

      // Data structure to hold names of subfolders to be examined for files.
      // Структура данных для хранения имен подпапок для проверки файлов.
      Stack<string> dirs = new Stack<string>();

      if (!Directory.Exists(root)) {
             throw new ArgumentException();
      }
      dirs.Push(root);

      while (dirs.Count > 0) {
         string currentDir = dirs.Pop();
         string[] subDirs = {};
         string[] files = {};

         try {
            subDirs = Directory.GetDirectories(currentDir);
         }
         // Thrown if we do not have discovery permission on the directory.
         // Брошено, если у нас нет разрешения на открытие в каталоге.
         catch (UnauthorizedAccessException e) {
            Console.WriteLine(e.Message);
            continue;
         }
         // Thrown if another process has deleted the directory after we retrieved its name.
         // если другой процесс удалил каталог после того, как мы получили его имя
         catch (DirectoryNotFoundException e) {
            Console.WriteLine(e.Message);
            continue;
         }

         try {
            files = Directory.GetFiles(currentDir);
         }
         catch (UnauthorizedAccessException e) {
            Console.WriteLine(e.Message);
            continue;
         }
         catch (DirectoryNotFoundException e) {
            Console.WriteLine(e.Message);
            continue;
         }
         catch (IOException e) {
            Console.WriteLine(e.Message);
            continue;
         }

         // Execute in parallel if there are enough files in the directory.
         // Otherwise, execute sequentially.Files are opened and processed
         // synchronously but this could be modified to perform async I/O.
         // Выполняем параллельно, если в каталоге достаточно файлов.
         // В противном случае выполните последовательно. Файлы открываются и обрабатываются
         // синхронно, но это может быть изменено для выполнения асинхронного ввода-вывода.
         try {
            if (files.Length < procCount) {
               foreach (var file in files) {
                  action(file);
                  fileCount++;                            
               }
            }
            else {
               Parallel.ForEach(files, () => 0, (file, loopState, localCount) =>
                                            { action(file);
                                              return (int) ++localCount;
                                            },
                                (c) => {
                                          Interlocked.Add(ref fileCount, c);                          
                                });
            }
         }
         catch (AggregateException ae) {
            ae.Handle((ex) => {
                         if (ex is UnauthorizedAccessException) {
                            // Here we just output a message and go on.
                            // Здесь мы просто выводим сообщение и продолжаем.
                            Console.WriteLine(ex.Message);
                            return true;
                         }
                         // Handle other exceptions here if necessary...
                         // При необходимости обрабатывайте другие исключения ...

                         return false;
            });
         }

         // Push the subdirectories onto the stack for traversal.
         // This could also be done before handing the files.
         // Вставьте подкаталоги в стек для обхода.
         // Это можно сделать и перед передачей файлов.
         foreach (string str in subDirs)
            dirs.Push(str);
      }

      // For diagnostic purposes.
      // Для диагностических целей.
      Console.WriteLine("Processed {0} files in {1} milliseconds", fileCount, sw.ElapsedMilliseconds);
   }
    }
}
