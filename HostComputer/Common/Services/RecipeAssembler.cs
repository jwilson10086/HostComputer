using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HostComputer.Models.RicipeEditor;

namespace HostComputer.Common.Services
{
    public static class RecipeAssembler
    {
        public static void BuildRowsFromRecipe(
            RecipeModel recipe,
            IEnumerable<UnitItemDefinition> definitions,
            ObservableCollection<RecipeParamRow> rows
        )
        {
            rows.Clear();

            foreach (var def in definitions)
            {
                var row = new RecipeParamRow { Definition = def };

                foreach (var step in recipe.Steps)
                {
                    step.Parameters.TryGetValue(def.Key, out var value);
                    row.StepValues.Add(new RecipeStepValue { Value = value });
                }

                rows.Add(row);
            }
        }

        public static RecipeModel BuildRecipeFromRows(
            ObservableCollection<RecipeParamRow> rows,
            int stepCount,
            string recipeName
        )
        {
            var recipe = new RecipeModel { UnitRecipeName = recipeName };

            for (int stepIndex = 0; stepIndex < stepCount; stepIndex++)
            {
                var step = new UnitStepModel { StepIndex = stepIndex + 1 };

                foreach (var row in rows)
                {
                    step.Parameters[row.Definition.Key] = row.StepValues[stepIndex].Value;
                }

                recipe.Steps.Add(step);
            }

            return recipe;
        }


        /// <summary>
        /// Recipe → UI 表格
        /// </summary>
        public static void BuildRows(
            RecipeModel recipe,
            IEnumerable<UnitItemDefinition> definitions,
            ObservableCollection<RecipeParamRow> rows,
            int stepCount)
        {
            rows.Clear();

            foreach (var def in definitions)
            {
                var row = new RecipeParamRow
                {
                    Item = def.DisplayName,   // UI 用
                    Definition = def          // 逻辑用
                };

                for (int i = 0; i < stepCount; i++)
                {
                    string value = string.Empty;

                    if (i < recipe.Steps.Count &&
                        recipe.Steps[i].Parameters.TryGetValue(def.Key, out var v))
                    {
                        value = v;
                    }

                    row.StepValues.Add(new RecipeStepValue { Value = value });
                }

                rows.Add(row);
            }
        }
    }
}
