using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NECS.Extensions;

[TestClass]
public class PriorityEventQueueTests
{
    [TestMethod]
    public void TestSingleThreadExecutionOrder()
    {
        var priorityOrder = new List<int> { 1, 2, 3 }; // Приоритеты: 1 (высший), 2, 3 (низший)
        var queue = new PriorityEventQueue<int, Action>(priorityOrder, gatesOpened: 1);

        var results = new List<int>();

        queue.AddEvent(1, () => results.Add(1));
        queue.AddEvent(2, () => results.Add(2));
        queue.AddEvent(3, () => results.Add(3));

        // Ждем завершения всех задач
        Task.Delay(100).Wait();

        // Проверяем порядок выполнения
        Assert.Equals(new List<int> { 1, 2, 3 }, results);
    }

    [TestMethod]
    public void TestMultiThreadExecutionOrder()
    {
        var priorityOrder = new List<int> { 1, 2, 3 };
        var queue = new PriorityEventQueue<int, Action>(priorityOrder, gatesOpened: 1);

        var results = new List<int>();
        object lockObj = new object();

        // Запускаем несколько потоков для добавления событий
        Parallel.Invoke(
            () => queue.AddEvent(1, () => { lock (lockObj) results.Add(1); }),
            () => queue.AddEvent(2, () => { lock (lockObj) results.Add(2); }),
            () => queue.AddEvent(3, () => { lock (lockObj) results.Add(3); })
        );

        // Ждем завершения всех задач
        Task.Delay(100).Wait();

        // Проверяем порядок выполнения
        Assert.Equals(new List<int> { 1, 2, 3 }, results);
    }

    [TestMethod]
    public void TestGatesCounter()
    {
        var priorityOrder = new List<int> { 1, 2, 3 };
        var queue = new PriorityEventQueue<int, Action>(priorityOrder, gatesOpened: 1, gatesCounter: x => x + 1);

        var results = new List<int>();
        object lockObj = new object();

        // Добавляем события
        queue.AddEvent(1, () => { lock (lockObj) results.Add(1); });
        queue.AddEvent(2, () => { lock (lockObj) results.Add(2); });
        queue.AddEvent(3, () => { lock (lockObj) results.Add(3); });

        // Ждем завершения всех задач
        Task.Delay(100).Wait();

        // Проверяем, что все события выполнены
        Assert.Equals(3, results.Count);
    }

    [TestMethod]
    public void TestConcurrentAddAndExecute()
    {
        var priorityOrder = new List<int> { 1, 2, 3 };
        var queue = new PriorityEventQueue<int, Action>(priorityOrder, gatesOpened: 1);

        var results = new List<int>();
        object lockObj = new object();

        // Запускаем несколько потоков для добавления и выполнения событий
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int priority = i % 3 + 1; // Приоритеты 1, 2, 3
            tasks.Add(Task.Run(() =>
            {
                queue.AddEvent(priority, () =>
                {
                    lock (lockObj)
                    {
                        results.Add(priority);
                    }
                });
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Ждем завершения всех задач
        Task.Delay(100).Wait();

        // Проверяем, что все события выполнены в правильном порядке
        Assert.Equals(10, results.Count);
        for (int i = 0; i < results.Count - 1; i++)
        {
            Assert.IsTrue(results[i] <= results[i + 1]); // Проверка порядка приоритетов
        }
    }

    [TestMethod]
    public void TestExceptionOnInvalidKey()
    {
        var priorityOrder = new List<int> { 1, 2, 3 };
        var queue = new PriorityEventQueue<int, Action>(priorityOrder);

        // Пытаемся добавить событие с недопустимым ключом
        Assert.ThrowsException<ArgumentException>(() => queue.AddEvent(4, () => { }));
    }
}