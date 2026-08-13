using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MegaCrit.Sts2.Core.Map;

/// <summary>
/// Handles the creation of map points for a map.
/// </summary>
public abstract class ActMap
{
	[CompilerGenerated]
	private sealed class <GetAllMapPoints>d__11 : IEnumerable<MapPoint>, IEnumerable, IEnumerator<MapPoint>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private MapPoint <>2__current;

		private int <>l__initialThreadId;

		public ActMap <>4__this;

		private int <c>5__2;

		private int <r>5__3;

		MapPoint IEnumerator<MapPoint>.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		[DebuggerHidden]
		public <GetAllMapPoints>d__11(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			int num = <>1__state;
			ActMap actMap = <>4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				<>1__state = -1;
				goto IL_0062;
			}
			<>1__state = -1;
			<c>5__2 = 0;
			goto IL_0096;
			IL_0072:
			if (<r>5__3 < actMap.Grid.GetLength(1))
			{
				MapPoint mapPoint = actMap.Grid[<c>5__2, <r>5__3];
				if (mapPoint != null)
				{
					<>2__current = mapPoint;
					<>1__state = 1;
					return true;
				}
				goto IL_0062;
			}
			<c>5__2++;
			goto IL_0096;
			IL_0062:
			<r>5__3++;
			goto IL_0072;
			IL_0096:
			if (<c>5__2 < actMap.GetColumnCount())
			{
				<r>5__3 = 0;
				goto IL_0072;
			}
			return false;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<MapPoint> IEnumerable<MapPoint>.GetEnumerator()
		{
			<GetAllMapPoints>d__11 result;
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				result = this;
			}
			else
			{
				result = new <GetAllMapPoints>d__11(0)
				{
					<>4__this = <>4__this
				};
			}
			return result;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MapPoint>)this).GetEnumerator();
		}
	}

	[CompilerGenerated]
	private sealed class <GetPointsInRow>d__12 : IEnumerable<MapPoint>, IEnumerable, IEnumerator<MapPoint>, IEnumerator, IDisposable
	{
		private int <>1__state;

		private MapPoint <>2__current;

		private int <>l__initialThreadId;

		private int row;

		public int <>3__row;

		public ActMap <>4__this;

		private int <c>5__2;

		MapPoint IEnumerator<MapPoint>.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		object IEnumerator.Current
		{
			[DebuggerHidden]
			get
			{
				return <>2__current;
			}
		}

		[DebuggerHidden]
		public <GetPointsInRow>d__12(int <>1__state)
		{
			this.<>1__state = <>1__state;
			<>l__initialThreadId = Environment.CurrentManagedThreadId;
		}

		[DebuggerHidden]
		void IDisposable.Dispose()
		{
			<>1__state = -2;
		}

		private bool MoveNext()
		{
			int num = <>1__state;
			ActMap actMap = <>4__this;
			if (num != 0)
			{
				if (num != 1)
				{
					return false;
				}
				<>1__state = -1;
				goto IL_0076;
			}
			<>1__state = -1;
			if (row >= 0 && row < actMap.Grid.GetLength(1))
			{
				<c>5__2 = 0;
				goto IL_0086;
			}
			goto IL_0094;
			IL_0086:
			if (<c>5__2 < actMap.GetColumnCount())
			{
				MapPoint mapPoint = actMap.Grid[<c>5__2, row];
				if (mapPoint != null)
				{
					<>2__current = mapPoint;
					<>1__state = 1;
					return true;
				}
				goto IL_0076;
			}
			goto IL_0094;
			IL_0094:
			return false;
			IL_0076:
			<c>5__2++;
			goto IL_0086;
		}

		bool IEnumerator.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			return this.MoveNext();
		}

		[DebuggerHidden]
		void IEnumerator.Reset()
		{
			throw new NotSupportedException();
		}

		[DebuggerHidden]
		IEnumerator<MapPoint> IEnumerable<MapPoint>.GetEnumerator()
		{
			<GetPointsInRow>d__12 <GetPointsInRow>d__;
			if (<>1__state == -2 && <>l__initialThreadId == Environment.CurrentManagedThreadId)
			{
				<>1__state = 0;
				<GetPointsInRow>d__ = this;
			}
			else
			{
				<GetPointsInRow>d__ = new <GetPointsInRow>d__12(0)
				{
					<>4__this = <>4__this
				};
			}
			<GetPointsInRow>d__.row = <>3__row;
			return <GetPointsInRow>d__;
		}

		[DebuggerHidden]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<MapPoint>)this).GetEnumerator();
		}
	}

	public readonly HashSet<MapPoint> startMapPoints = new HashSet<MapPoint>();

	public abstract MapPoint BossMapPoint { get; }

	public abstract MapPoint StartingMapPoint { get; }

	/// <summary>
	/// The second boss map point for Double Boss mode (Ascension 10+).
	/// Null if not in double boss mode.
	/// </summary>
	public virtual MapPoint? SecondBossMapPoint => null;

	protected abstract MapPoint?[,] Grid { get; }

	public int GetColumnCount()
	{
		return Grid.GetLength(0);
	}

	public int GetRowCount()
	{
		return Grid.GetLength(1);
	}

	[IteratorStateMachine(typeof(<GetAllMapPoints>d__11))]
	public IEnumerable<MapPoint> GetAllMapPoints()
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <GetAllMapPoints>d__11(-2)
		{
			<>4__this = this
		};
	}

	[IteratorStateMachine(typeof(<GetPointsInRow>d__12))]
	public IEnumerable<MapPoint> GetPointsInRow(int row)
	{
		//yield-return decompiler failed: Unexpected instruction in Iterator.Dispose()
		return new <GetPointsInRow>d__12(-2)
		{
			<>4__this = this,
			<>3__row = row
		};
	}

	public virtual MapPoint? GetPoint(MapCoord coord)
	{
		return GetPoint(coord.col, coord.row);
	}

	public MapPoint? GetPoint(int col, int row)
	{
		if (col == BossMapPoint.coord.col && row == BossMapPoint.coord.row)
		{
			return BossMapPoint;
		}
		if (SecondBossMapPoint != null && col == SecondBossMapPoint.coord.col && row == SecondBossMapPoint.coord.row)
		{
			return SecondBossMapPoint;
		}
		if (col == StartingMapPoint.coord.col && row == StartingMapPoint.coord.row)
		{
			return StartingMapPoint;
		}
		if (col >= 0 && col < Grid.GetLength(0) && row >= 0 && row < Grid.GetLength(1))
		{
			return Grid[col, row];
		}
		return null;
	}

	public bool IsInMap(MapPoint mapPoint)
	{
		if (mapPoint.PointType == MapPointType.Ancient || mapPoint.PointType == MapPointType.Boss)
		{
			return true;
		}
		int col = mapPoint.coord.col;
		int row = mapPoint.coord.row;
		if (col < 0 || col >= Grid.GetLength(0) || row < 0 || row >= Grid.GetLength(1))
		{
			return false;
		}
		return Grid[col, row] != null;
	}

	public bool HasPoint(MapCoord coord)
	{
		if (coord.col == BossMapPoint.coord.col && coord.row == BossMapPoint.coord.row)
		{
			return true;
		}
		if (SecondBossMapPoint != null && coord.col == SecondBossMapPoint.coord.col && coord.row == SecondBossMapPoint.coord.row)
		{
			return true;
		}
		if (coord.col == StartingMapPoint.coord.col && coord.row == StartingMapPoint.coord.row)
		{
			return true;
		}
		if (coord.col < 0 || coord.col >= Grid.GetLength(0))
		{
			return false;
		}
		if (coord.row < 0 || coord.row >= Grid.GetLength(1))
		{
			return false;
		}
		return Grid[coord.col, coord.row] != null;
	}
}
You are not using the latest version of the tool, please update.
Latest version is '11.0.0.9375' (yours is '8.2.0.7535-95108c96')
