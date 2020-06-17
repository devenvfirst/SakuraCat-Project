// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using UnityEngine.UIElements;

namespace Naninovel
{
    public class PaginationView : VisualElement
    {
        private readonly Label label;

        public PaginationView (Action nextPageSelected, Action previousPageSelected)
        {
            styleSheets.Add(ScriptView.StyleSheet);
            if (ScriptView.CustomStyleSheet != null)
                styleSheets.Add(ScriptView.CustomStyleSheet);

            var prevButton = new Button(previousPageSelected);
            prevButton.text = "<";
            Add(prevButton);

            label = new Label();
            Add(label);

            var nextButton = new Button(nextPageSelected);
            nextButton.text = ">";
            Add(nextButton);
        }

        public void SetLabel (string value)
        {
            label.text = value;
        }
    }

}