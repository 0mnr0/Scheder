namespace Scheder.Tools;

using System;
using System.Threading.Tasks;

public static class ParallelTasks
{
    public static async Task<(T1, T2)> Run<T1, T2>(
        Task<T1> task1,
        Task<T2> task2)
    {
        await Task.WhenAll(task1, task2);
        return (task1.Result, task2.Result);
    }

    public static async Task<(T1, T2, T3)> Run<T1, T2, T3>(
        Task<T1> task1,
        Task<T2> task2,
        Task<T3> task3)
    {
        await Task.WhenAll(task1, task2, task3);
        return (task1.Result, task2.Result, task3.Result);
    }

    public static async Task<(T1, T2, T3, T4)> Run<T1, T2, T3, T4>(
        Task<T1> task1,
        Task<T2> task2,
        Task<T3> task3,
        Task<T4> task4)
    {
        await Task.WhenAll(task1, task2, task3, task4);
        return (task1.Result, task2.Result, task3.Result, task4.Result);
    }

    public static async Task<(T1, T2, T3, T4, T5)> Run<T1, T2, T3, T4, T5>(
        Task<T1> task1,
        Task<T2> task2,
        Task<T3> task3,
        Task<T4> task4,
        Task<T5> task5)
    {
        await Task.WhenAll(task1, task2, task3, task4, task5);
        return (task1.Result, task2.Result, task3.Result, task4.Result, task5.Result);
    }
}