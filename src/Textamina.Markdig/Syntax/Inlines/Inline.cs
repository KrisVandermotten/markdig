﻿using System;
using System.Collections.Generic;
using Textamina.Markdig.Parsing;

namespace Textamina.Markdig.Syntax
{
    public abstract class Inline
    {
        public Inline Parent { get; internal set; }

        public Inline PreviousSibling { get; private set; }

        public Inline NextSibling { get; internal set; }

        public void InsertAfter(Inline next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (next.Parent != null)
            {
                throw new ArgumentException("Inline has already a parent", nameof(next));
            }

            var previousNext = NextSibling;
            if (previousNext != null)
            {
                previousNext.PreviousSibling = next;
            }

            next.PreviousSibling = this;
            next.NextSibling = previousNext;
            NextSibling = next;

            if (Parent != null)
            {
                Parent.OnChildInsert(next);
                next.Parent = Parent;
            }
        }

        public void InsertBefore(Inline previous)
        {
            if (previous == null) throw new ArgumentNullException(nameof(previous));
            if (previous.Parent != null)
            {
                throw new ArgumentException("Inline has already a parent", nameof(previous));
            }

            var previousSibling = PreviousSibling;
            if (previousSibling != null)
            {
                previousSibling.NextSibling = previous;
            }

            PreviousSibling = previous;
            previous.NextSibling = this;

            if (Parent != null)
            {
                Parent.OnChildInsert(previous);
                previous.Parent = Parent;
            }
        }

        /// <summary>
        /// Removes this instance from the current list and its parent
        /// </summary>
        public void Remove()
        {
            if (PreviousSibling != null)
            {
                PreviousSibling.NextSibling = NextSibling;
            }

            if (NextSibling != null)
            {
                NextSibling.PreviousSibling = PreviousSibling;
            }

            if (Parent != null)
            {
                Parent.OnChildRemove(this);

                PreviousSibling = null;
                NextSibling = null;
                Parent = null;
            }
        }

        public Inline ReplaceBy(Inline inline)
        {
            if (inline == null) throw new ArgumentNullException(nameof(inline));

            // Save sibling
            var parent = Parent;
            var previousSibling = PreviousSibling;
            var nextSibling = NextSibling;
            Remove();

            if (previousSibling != null)
            {
                previousSibling.InsertAfter(inline);
            }
            else if (nextSibling != null)
            {
                nextSibling.InsertBefore(inline);
            }
            else if (Parent != null)
            {
                ((ContainerInline)Parent).AppendChild(inline);
            }

            var container = this as ContainerInline;
            if (container != null)
            {
                var newContainer = inline as ContainerInline;
                // TODO: This part is not efficient as it is using child.Remove()
                // We need a method to quickly move all children without having to mess Next/Prev sibling
                var child = container.FirstChild;
                var lastChild = child;
                while (child != null)
                {
                    lastChild = child;
                    var nextChild = child.NextSibling;
                    child.Remove();
                    if (newContainer != null)
                    {
                        newContainer.AppendChild(child);
                    }
                    else
                    {
                        inline.InsertAfter(child);
                    }
                    child = nextChild;
                }

                return lastChild;
            }

            return inline;
        }

        public bool ContainsParentOfType<T>() where T : Inline
        {
            var delimiter = this as T;
            if (delimiter != null)
            {
                return true;
            }

            if (Parent != null)
            {
                return Parent.ContainsParentOfType<T>();
            }

            return false;
        }

        public IEnumerable<T> FindParentOfType<T>() where T : Inline
        {
            var delimiter = this as T;
            if (delimiter != null)
            {
                yield return delimiter;
            }

            if (Parent != null)
            {
                foreach (var parent in Parent.FindParentOfType<T>())
                {
                    yield return parent;
                }
            }
        }

        protected virtual void OnChildRemove(Inline child)
        {
            
        }

        protected virtual void OnChildInsert(Inline child)
        {
        }

        protected internal virtual void Close(MatchInlineState state)
        {
        }
    }
}