//
//	C# Thread Safe Circular Queue Sample Implementation
//	Copyright ©2008 Leniel Braz de Oliveira Macaferi.
//
//  This program sample was developed and turned in for a test regarding a
//  Software Development Engineer in Test (SDTE) position at Microsoft 
//  The source code is provided "as is" without warranty.
//
//  References
//  Thread Safety articles
//  [1] Venners, Bill. Designing for Thread Safety: Using Synchronization, Immutable Objects, and Thread-Safe Wrappers. 1998.
//  Available at <http://www.artima.com/designtechniques/threadsafety.html>. Accessed on April 29, 2008.
//
//  [2] Suess, Michael. A Short Guide to Mastering Thread-Safety. 2006.
//  Available at <http://www.thinkingparallel.com/2006/10/15/a-short-guide-to-mastering-thread-safety/>. Accessed on April 29, 2008.
//
//  [3] Allen, K. Scott. Statics & Thread Safety: Part II. 2004.
//  Available at <http://www.odetocode.com/Articles/314.aspx>. Accessed on April 29, 2008.
//
//  Circular Queue sample code
//  [4] Kumar, Nunna Santhosh. Circular Queue Implementation using Arrays in C++.
//  Available at <http://www.sourcecodesworld.com/source/show.asp?ScriptID=887>. Accessed on April 29, 2008.
//
//  Thread material
//  [5] Albahari, Joseph. Threading in C#. 2007.
//  Available at <http://www.albahari.com/threading/>. Accessed on April 29, 2008.
//
//  January 2008

using System;

namespace SteamVRRecorder
{
    public class ThreadSafeCircularQueue<T>
    {
        private readonly T[] _queue;
        private int _head;
        private int _tail;
        private readonly int _length;
        private readonly T _zero;

        private static readonly Object ThisLock = new Object();

        public ThreadSafeCircularQueue(int length, T zero)
        {
            _zero = zero;
            _head = _tail = -1;
            _length = length;
            _queue = new T[_length];
        }

        public void Put(T value)
        {
            lock (ThisLock)
            {
                if ((_head == 0 && _tail == _length - 1) || (_tail + 1 == _head))
                {
                    Console.WriteLine("Circular queue is full.");

                    return;
                }
                else
                {
                    if (_tail == _length - 1)
                        _tail = 0;
                    else
                        _tail++;

                    _queue[_tail] = value;

                    Console.WriteLine("In -> {0}", value);
                }

                if (_head == -1)
                    _head = 0;
            }
        }

        public bool IsEmpty()
        {
            return _head == _tail;
        }

        public T Pop()
        {
            lock (ThisLock)
            {
                T value;

                if (_head == _tail)
                {
                    Console.WriteLine("Circular queue is empty.");
                    value = _zero;
                }
                else
                {
                    value = _queue[_head];
                    _queue[_head] = _zero;

                    if (_head == _tail)
                        _head = _tail = -1;
                    else
                    if (_head == _length - 1)
                        _head = 0;
                    else
                        _head++;
                }

                return value;
            }
        }
    }
}