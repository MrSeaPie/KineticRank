using NUnit.Framework;
using KineticRank.Logic;

namespace KineticRank.Tests.EditMode
{
    [TestFixture]
    public class BoardEvaluatorTests
    {
        [Test]
        public void Evaluate_NullOrEmptyGrid_ReturnsInProgress()
        {
            Assert.AreEqual(EvaluationResult.InProgress, BoardEvaluator.Evaluate(null));
            int[,] emptyGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            Assert.AreEqual(EvaluationResult.InProgress, BoardEvaluator.Evaluate(emptyGrid));
        }

        [Test]
        public void Evaluate_HorizontalLine_DetectsWinnerForBothColors()
        {
            int[,] redGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int c = 1; c <= 4; c++) redGrid[c, 0] = (int)CellOwner.Red;
            Assert.AreEqual(EvaluationResult.RedWin, BoardEvaluator.Evaluate(redGrid));

            int[,] blueGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int c = 2; c <= 5; c++) blueGrid[c, 2] = (int)CellOwner.Blue;
            Assert.AreEqual(EvaluationResult.BlueWin, BoardEvaluator.Evaluate(blueGrid));
        }

        [Test]
        public void Evaluate_VerticalLine_DetectsWinnerForBothColors()
        {
            int[,] redGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int r = 0; r < 4; r++) redGrid[0, r] = (int)CellOwner.Red;
            Assert.AreEqual(EvaluationResult.RedWin, BoardEvaluator.Evaluate(redGrid));

            int[,] blueGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int r = 1; r < 5; r++) blueGrid[3, r] = (int)CellOwner.Blue;
            Assert.AreEqual(EvaluationResult.BlueWin, BoardEvaluator.Evaluate(blueGrid));
        }

        [Test]
        public void Evaluate_DiagonalUpRight_DetectsWinnerForBothColors()
        {
            int[,] redGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int i = 0; i < 4; i++) redGrid[i, i] = (int)CellOwner.Red;
            Assert.AreEqual(EvaluationResult.RedWin, BoardEvaluator.Evaluate(redGrid));

            int[,] blueGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int i = 0; i < 4; i++) blueGrid[2 + i, 1 + i] = (int)CellOwner.Blue;
            Assert.AreEqual(EvaluationResult.BlueWin, BoardEvaluator.Evaluate(blueGrid));
        }

        [Test]
        public void Evaluate_DiagonalDownRight_DetectsWinnerForBothColors()
        {
            int[,] redGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int i = 0; i < 4; i++) redGrid[i, 3 - i] = (int)CellOwner.Red;
            Assert.AreEqual(EvaluationResult.RedWin, BoardEvaluator.Evaluate(redGrid));

            int[,] blueGrid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int i = 0; i < 4; i++) blueGrid[1 + i, 5 - i] = (int)CellOwner.Blue;
            Assert.AreEqual(EvaluationResult.BlueWin, BoardEvaluator.Evaluate(blueGrid));
        }

        [Test]
        public void Evaluate_SimultaneousLines_ResolvesToDraw()
        {
            int[,] grid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int c = 0; c < 4; c++) grid[c, 0] = (int)CellOwner.Red;
            for (int c = 0; c < 4; c++) grid[c, 1] = (int)CellOwner.Blue;
            Assert.AreEqual(EvaluationResult.Draw, BoardEvaluator.Evaluate(grid));
        }

        [Test]
        public void Evaluate_FullBoardWithoutLines_ReturnsInProgressForDownshiftMechanic()
        {
            int[,] grid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int c = 0; c < BoardEvaluator.Columns; c++)
            {
                for (int r = 0; r < BoardEvaluator.Rows; r++)
                {
                    grid[c, r] = ((c / 2 + r) % 2 == 0) ? (int)CellOwner.Red : (int)CellOwner.Blue;
                }
            }
            Assert.IsFalse(BoardEvaluator.HasWinningLine(grid, (int)CellOwner.Red));
            Assert.IsFalse(BoardEvaluator.HasWinningLine(grid, (int)CellOwner.Blue));
            Assert.AreEqual(EvaluationResult.InProgress, BoardEvaluator.Evaluate(grid));
        }

        [Test]
        public void Evaluate_ThreeInARow_ReturnsInProgress()
        {
            int[,] grid = new int[BoardEvaluator.Columns, BoardEvaluator.Rows];
            for (int c = 0; c < 3; c++) grid[c, 0] = (int)CellOwner.Red;
            Assert.AreEqual(EvaluationResult.InProgress, BoardEvaluator.Evaluate(grid));
        }
    }
}