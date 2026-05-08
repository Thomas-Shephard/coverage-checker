using CoverageChecker.Results;

namespace CoverageChecker.Tests.Unit.ResultTests;

public class RegressionResultTests
{
    [Test]
    public void ConstructorWithNullRegressedFilesInitializesEmpty()
    {
        RegressionResult result = new(null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.RegressedFiles, Is.Empty);
            Assert.That(result.HasRegressions, Is.False);
        }
    }

    [Test]
    public void ConstructorWithEmptyRegressedFilesInitializesEmpty()
    {
        RegressionResult result = new([]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.RegressedFiles, Is.Empty);
            Assert.That(result.HasRegressions, Is.False);
        }
    }
}
