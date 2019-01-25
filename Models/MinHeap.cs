using System;
using System.Collections.Generic;

namespace hlcup2018.Models
{
  public class MinHeap<T>
  {
      private readonly IComparer<T> _comparer;
      private readonly T[] _elements;
      private int _size;

      public MinHeap(int size, IComparer<T> cmp)
      {
          _elements = new T[size];
          _comparer = cmp;
      }

      private int GetLeftChildIndex(int elementIndex) {return 2 * elementIndex + 1;}
      private int GetRightChildIndex(int elementIndex) {return 2 * elementIndex + 2;}
      private int GetParentIndex(int elementIndex) {return (elementIndex - 1) / 2;}

      private bool HasLeftChild(int elementIndex) {return GetLeftChildIndex(elementIndex) < _size;}
      private bool HasRightChild(int elementIndex) {return GetRightChildIndex(elementIndex) < _size;}
      private bool IsRoot(int elementIndex) {return elementIndex == 0;}

      private T GetLeftChild(int elementIndex) {return _elements[GetLeftChildIndex(elementIndex)];}
      private T GetRightChild(int elementIndex) {return _elements[GetRightChildIndex(elementIndex)];}
      private T GetParent(int elementIndex) {return _elements[GetParentIndex(elementIndex)];}

      private void Swap(int firstIndex, int secondIndex)
      {
          var temp = _elements[firstIndex];
          _elements[firstIndex] = _elements[secondIndex];
          _elements[secondIndex] = temp;
      }

      public int Count { get { return _size; } }

      public T[] ToSorted()
      {
          var lastElement = _size - 1;
          
          while(lastElement > 0) 
          {
            Swap(0, lastElement);
            _size--;
            lastElement--;
            ReCalculateDown();
          }

          return _elements;
      }

      public bool IsEmpty()
      {
          return _size == 0;
      }

      public T Peek()
      {
          if (_size == 0)
              throw new IndexOutOfRangeException();

          return _elements[0];
      }

      public T Pop()
      {
          if (_size == 0)
              throw new IndexOutOfRangeException();

          var result = _elements[0];
          _elements[0] = _elements[_size - 1];
          _size--;

          ReCalculateDown();

          return result;
      }

      public void Add(T element)
      {
          if (_size == _elements.Length)
              throw new IndexOutOfRangeException();

          _elements[_size] = element;
          _size++;

          ReCalculateUp();
      }

      private void ReCalculateDown()
      {
          int index = 0;
          while (HasLeftChild(index))
          {
              var smallerIndex = GetLeftChildIndex(index);
              if (HasRightChild(index) && _comparer.Compare(GetRightChild(index), GetLeftChild(index)) < 0)
              {
                  smallerIndex = GetRightChildIndex(index);
              }

              var cmp = _comparer.Compare(_elements[smallerIndex], _elements[index]);
              if (cmp >= 0)
              {
                  break;
              }

              Swap(smallerIndex, index);
              index = smallerIndex;
          }
      }

      private void ReCalculateUp()
      {
          var index = _size - 1;
          while (!IsRoot(index) && _comparer.Compare(_elements[index], GetParent(index)) < 0)
          {
              var parentIndex = GetParentIndex(index);
              Swap(parentIndex, index);
              index = parentIndex;
          }
      }
  }  
}