﻿using System;
using System.Collections.Generic;

namespace Glory
{
    /// <summary>
    /// An enumerator that provides lookahead without advancing the cursor
    /// </summary>
    /// <typeparam name="T">The type to enumerate</typeparam>

    class LookAheadEnumerator : object, IEnumerator<Token>
    {
        private const int _Enumerating = 0;
        private const int _NotStarted = -2;
        private const int _Ended = -1;
        private const int _Disposed = -3;
        private IEnumerator<Token> _inner;
        private int _state;
        // for the lookahead queue
        private const int _DefaultCapacity = 16;
        private const float _GrowthFactor = 0.9F;
        private Token[] _queue;
        private int _queueHead;
        private int _queueCount;
        /// <summary>
        /// Creates a new instance. Once this is created, the inner/wrapped enumerator must not be touched.
        /// </summary>
        /// <param name="inner"></param>
        public LookAheadEnumerator(IEnumerator<Token> inner)
        {
            this._inner = inner;
            this._state = _NotStarted;
            this._queue = new Token[_DefaultCapacity];
            this._queueHead = 0;
            this._queueCount = 0;
        }
        /// <summary>
        /// Discards the lookahead and advances the cursor to the physical position.
        /// </summary>
        public void DiscardLookAhead()
        {
            for (
            ; (1 < this._queueCount);
            )
            {
                this._Dequeue();
            }
        }
        /// <summary>
        /// Retrieves the value under the cursor
        /// </summary>
        public Token Current {
            get {
                if (0 > this._state)
                {
                    if (_NotStarted == this._state)
                    {
                        throw new InvalidOperationException("The cursor is before the start of the enumeration.");
                    }
                    if (_Ended == this._state)
                    {
                        throw new InvalidOperationException("The cursor is after the end of the enumeration.");
                    }
                    throw new ObjectDisposedException(typeof(LookAheadEnumerator).Name);
                }
                return this._queue[this._queueHead];
            }
        }
        // legacy enum support (required)
        object System.Collections.IEnumerator.Current {
            get {
                return this.Current;
            }
        }
        internal int QueueCount {
            get {
                return this._queueCount;
            }
        }
        /// <summary>
        /// Attempts to peek the specified number of positions from the current position without advancing
        /// </summary>
        /// <param name="lookahead">The offset from the current position to peek at</param>
        /// <param name="value">The value returned</param>
        /// <returns>True if the peek could be satisfied, otherwise false</returns>
        public bool TryPeek(int lookahead, out Token value)
        {
            if (_Disposed == this._state)
            {
                throw new ObjectDisposedException(typeof(LookAheadEnumerator).Name);
            }
            if (0 > lookahead)
            {
                throw new ArgumentOutOfRangeException("lookahead");
            }
            if (_Ended == this._state)
            {
                value = default(Token);
                return false;
            }
            if (_NotStarted == this._state)
            {
                if ((0 == lookahead))
                {
                    value = default(Token);
                    return false;
                }
            }
            if (lookahead < this._queueCount)
            {
                value = this._queue[((lookahead + this._queueHead)
                            % this._queue.Length)];
                return true;
            }
            lookahead = (lookahead - this._queueCount);
            value = default(Token);
            for (
            ; ((0 <= lookahead)
                        && this._inner.MoveNext());
            )
            {
                value = this._inner.Current;
                this._Enqueue(value);
                lookahead = (lookahead - 1);
            }
            return (-1 == lookahead);
        }
        /// <summary>
        /// Peek the specified number of positions from the current position without advancing
        /// </summary>
        /// <param name="lookahead">The offset from the current position to peek at</param>
        /// <returns>The value at the specified position</returns>
        public Token Peek(int lookahead)
        {
            Token value;
            if ((false == this.TryPeek(lookahead, out value)))
            {
                throw new InvalidOperationException("There were not enough values in the enumeration to satisfy the request");
            }
            return value;
        }
        internal bool IsEnumerating {
            get {
                return (-1 < this._state);
            }
        }
        internal bool IsEnded {
            get {
                return (_Ended == this._state);
            }
        }
        /// <summary>
        /// Retrieves a lookahead cursor from the current cursor that can be navigated without moving the main cursor
        /// </summary>
        public IEnumerable<Token> LookAhead {
            get {
                if ((0 > this._state))
                {
                    if ((this._state == _NotStarted))
                    {
                        throw new InvalidOperationException("The cursor is before the start of the enumeration.");
                    }
                    if ((this._state == _Ended))
                    {
                        throw new InvalidOperationException("The cursor is after the end of the enumeration.");
                    }
                    throw new ObjectDisposedException(typeof(LookAheadEnumerator).Name);
                }
                return new LookAheadEnumeratorEnumerable(this);
            }
        }
        public bool MoveNext()
        {
            if ((0 > this._state))
            {
                if ((_Disposed == this._state))
                {
                    throw new ObjectDisposedException(typeof(LookAheadEnumerator).Name);
                }
                if ((_Ended == this._state))
                {
                    return false;
                }
                if ((_NotStarted == this._state))
                {
                    if ((0 < this._queueCount))
                    {
                        this._state = _Enumerating;
                        return true;
                    }
                    if ((false == this._inner.MoveNext()))
                    {
                        this._state = _Ended;
                        return false;
                    }
                    this._Enqueue(this._inner.Current);
                    this._state = _Enumerating;
                    return true;
                }
            }
            this._Dequeue();
            if ((0 == this._queueCount))
            {
                if ((false == this._inner.MoveNext()))
                {
                    this._state = _Ended;
                    return false;
                }
                this._Enqueue(this._inner.Current);
            }
            return true;
        }
        /// <summary>
        /// Advances the cursor
        /// </summary>
        /// <returns>True if more input was read, otherwise false</returns>
        bool System.Collections.IEnumerator.MoveNext()
        {
            return MoveNext();
        }
        /// <summary>
        /// Resets the cursor, and clears the queue.
        /// </summary>
        void System.Collections.IEnumerator.Reset()
        {
            this._inner.Reset();
            this._queueHead = 0;
            this._queueCount = 0;
            this._state = _NotStarted;
        }
#region IDisposable Support
        /// <summary>
        /// Disposes of this instance
        /// </summary>
        void System.IDisposable.Dispose()
        {
            if ((false
                        == (_Disposed == this._state)))
            {
                this._inner.Dispose();
                this._state = _Disposed;
            }
        }
        void _Enqueue(Token item)
        {
            if ((this._queueCount == this._queue.Length))
            {
                Token[] arr = new Token[((int)((this._queue.Length
                            * (1 + _GrowthFactor))))];
                if (((this._queueHead + this._queueCount)
                            <= this._queue.Length))
                {
                    System.Array.Copy(this._queue, arr, this._queueCount);
                    this._queueHead = 0;
                    arr[this._queueCount] = item;
                    this._queueCount = (this._queueCount + 1);
                    this._queue = arr;
                }
                else
                {
                    System.Array.Copy(this._queue, this._queueHead, arr, 0, (this._queue.Length - this._queueHead));
                    System.Array.Copy(this._queue, 0, arr, (this._queue.Length - this._queueHead), this._queueHead);
                    this._queueHead = 0;
                    arr[this._queueCount] = item;
                    this._queueCount = (this._queueCount + 1);
                    this._queue = arr;
                }
            }
            else
            {
                this._queue[((this._queueHead + this._queueCount)
                            % this._queue.Length)] = item;
                this._queueCount = (this._queueCount + 1);
            }
        }
        Token _Dequeue()
        {
            if ((0 == this._queueCount))
            {
                throw new InvalidOperationException("The queue is empty");
            }
            Token result = this._queue[this._queueHead];
            this._queue[this._queueHead] = default(Token);
            this._queueHead = (this._queueHead + 1);
            this._queueHead = (this._queueHead % this._queue.Length);
            this._queueCount = (this._queueCount - 1);
            return result;
        }
#endregion
    }
    internal class LookAheadEnumeratorEnumerable : object, IEnumerable<Token>
    {
        private LookAheadEnumerator _outer;
        public LookAheadEnumeratorEnumerable(LookAheadEnumerator outer)
        {
            this._outer = outer;
        }
        public IEnumerator<Token> GetEnumerator()
        {
            // for some reason VB was resolving new as AddressOf, so use this.
            LookAheadEnumeratorEnumerator result = ((LookAheadEnumeratorEnumerator)(System.Activator.CreateInstance(typeof(LookAheadEnumeratorEnumerator), this._outer)));
            return result;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
    internal class LookAheadEnumeratorEnumerator : object, IEnumerator<Token>
    {
        private const int _NotStarted = -2;
        private const int _Ended = -1;
        private const int _Disposed = -3;
        private LookAheadEnumerator _outer;
        private int _index;
        private Token _current;
        public LookAheadEnumeratorEnumerator(LookAheadEnumerator outer)
        {
            this._outer = outer;
            if (this._outer.IsEnumerating)
            {
                this._current = this._outer.Current;
            }
            this._index = _NotStarted;
        }
        public Token Current {
            get {
                if ((0 > this._index))
                {
                    if ((this._index == _NotStarted))
                    {
                        throw new InvalidOperationException("The cursor is before the start of the enumeration.");
                    }
                    if ((this._index == _Ended))
                    {
                        throw new InvalidOperationException("The cursor is after the end of the enumeration.");
                    }
                    throw new ObjectDisposedException(typeof(LookAheadEnumeratorEnumerator).Name);
                }
                return this._current;
            }
        }
        object System.Collections.IEnumerator.Current {
            get {
                return this.Current;
            }
        }
        void System.IDisposable.Dispose()
        {
            this._index = _Disposed;
        }
        bool System.Collections.IEnumerator.MoveNext()
        {
            Token value;
            if ((0 > this._index))
            {
                if ((this._index == _Disposed))
                {
                    throw new ObjectDisposedException(typeof(LookAheadEnumeratorEnumerator).Name);
                }
                if ((this._index == _Ended))
                {
                    return false;
                }
                this._index = -1;
            }
            this._index = (this._index + 1);
            if ((false == this._outer.TryPeek(this._index, out value)))
            {
                this._index = _Ended;
                return false;
            }
            this._current = value;
            return true;
        }
        void System.Collections.IEnumerator.Reset()
        {
            this._index = _NotStarted;
        }
    }
}
