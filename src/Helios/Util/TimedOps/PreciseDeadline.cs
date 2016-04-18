﻿using System;

namespace Helios.Util.TimedOps
{
    /// <summary>
    /// A <see cref="Deadline"/> alternative which relies on the <see cref="MonotonicClock"/> internally.
    /// </summary>
    internal struct PreciseDeadline
    {
        public PreciseDeadline(long tickCountDue)
        {
            When = tickCountDue;
        }

        public long When { get; }

        public bool IsOverdue => MonotonicClock.GetTicks() > When;

        #region Equality members

        public bool Equals(PreciseDeadline other)
        {
            return When == other.When;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is PreciseDeadline && Equals((PreciseDeadline)obj);
        }

        public override int GetHashCode()
        {
            return When.GetHashCode();
        }

        public static bool operator ==(PreciseDeadline left, PreciseDeadline right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PreciseDeadline left, PreciseDeadline right)
        {
            return !left.Equals(right);
        }

        #endregion

        #region Static members

        public static PreciseDeadline Now => new PreciseDeadline(MonotonicClock.GetTicks());

        public static readonly PreciseDeadline Never = new PreciseDeadline(DateTime.MaxValue.Ticks);

        public static readonly PreciseDeadline Zero = new PreciseDeadline(0);

        /// <summary>
        /// Adds a given <see cref="TimeSpan"/> to the due time of this <see cref="PreciseDeadline"/>
        /// </summary>
        public static PreciseDeadline operator +(PreciseDeadline deadline, TimeSpan duration)
        {
            return deadline == Never ? deadline : new PreciseDeadline(deadline.When + duration.Ticks);
        }

        /// <summary>
        /// Adds a given <see cref="Nullable{TimeSpan}"/> to the due time of this <see cref="PreciseDeadline"/>
        /// </summary>
        public static PreciseDeadline operator +(PreciseDeadline deadline, TimeSpan? duration)
        {
            if (duration.HasValue)
                return deadline + duration.Value;
            else
                return deadline;
        }

        /// <summary>
        /// Adds a given <see cref="TimeSpan"/> to the due time of this <see cref="PreciseDeadline"/>
        /// </summary>
        public static PreciseDeadline operator -(PreciseDeadline deadline, TimeSpan duration)
        {
            return deadline == Zero ? deadline : new PreciseDeadline(deadline.When - duration.Ticks);
        }

        /// <summary>
        /// Adds a given <see cref="Nullable{TimeSpan}"/> to the due time of this <see cref="PreciseDeadline"/>
        /// </summary>
        public static PreciseDeadline operator -(PreciseDeadline deadline, TimeSpan? duration)
        {
            if (duration.HasValue)
                return deadline - duration.Value;
            else
                return deadline;
        }


        #endregion
    }
}