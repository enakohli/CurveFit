using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CurveFit.Models;
using MathNet.Numerics;

namespace CurveFit.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public List<PointModel> PointModels { get; private set; } = new List<PointModel>();
    public List<PointModel> BestFitCurve { get; private set; } = new List<PointModel>();
    public string EquationOfBestFit { get; private set; } = "";

    public void OnGetAsync(string equation)
    {
        //some starting points
        PointModels.Add(new PointModel { X = 1, Y = 2 });
        PointModels.Add(new PointModel { X = 3, Y = 7 });
        PointModels.Add(new PointModel { X = 5, Y = 6 });
        PointModels.Add(new PointModel { X = 6, Y = 9 });
        PointModels.Add(new PointModel { X = 7, Y = 12 });
        PointModels.Add(new PointModel { X = 9, Y = 20 });

        EquationOfBestFit = equation;
    }
    public IActionResult OnPostFitCurve(string curveType, string points)
    {
        Console.WriteLine("Inside OnPostFitCurve");

        var parsedPoints = ParsePoints(points);
        if (PointModels == null)
        {
            PointModels = new List<PointModel>();
        }
        PointModels.AddRange(parsedPoints);
        Console.WriteLine($"Parsed Points: {string.Join(", ", PointModels.Select(p => $"({p.X}, {p.Y})"))}");

        BestFitCurve = CalculateBestFit(PointModels, curveType);

        if (curveType == "linear")
        {
            double slope = CalculateSlope(PointModels.Select(p => p.X).ToArray(), PointModels.Select(p => p.Y).ToArray());
            double intercept = CalculateIntercept(PointModels.Select(p => p.X).ToArray(), PointModels.Select(p => p.Y).ToArray());

            EquationOfBestFit = EquationOfLine(slope, intercept);
        }

        else if (curveType == "quadratic")
        {
            EquationOfBestFit = EquationOfQuadratic(PointModels);
        }

        else if (curveType == "cubic")
        {
            EquationOfBestFit = EquationOfCubic(PointModels);
        }

        return RedirectToPage(new { equation = EquationOfBestFit });
    }

    private List<PointModel> ParsePoints(string points)
    {
        var result = new List<PointModel>();

        if (!string.IsNullOrEmpty(points))
        {
            var pairs = points.Split(' ', '\n', '\t').Select(pair => pair.Trim()).Where(pair => !string.IsNullOrEmpty(pair)).Select(pair =>
            {
                var coordinates = pair.Split(',');
                if (double.TryParse(coordinates[0], out var x) &&
                    double.TryParse(coordinates[1], out var y) && coordinates.Length == 2)
                {
                    return new PointModel { X = x, Y = y };
                }
                else
                {
                    return null;
                }
            }).Where(point => point != null).ToList();
            result.AddRange(pairs);
        }
        return result;
    }

    private string EquationOfLine(double slope, double intercept)
    {
        return $"y = {slope}x + {intercept}";
    }

    private string EquationOfQuadratic(List<PointModel> points)
    {
        double a, b, c;
        QuadraticRegression(points.Select(p => p.X).ToArray(), points.Select(p => p.Y).ToArray(), out a, out b, out c);

        return $"y = {a}x^2 + {b}x + {c}";
    }

    private string EquationOfCubic(List<PointModel> points)
    {
        double a, b, c, d;
        CubicRegression(points.Select(p => p.X).ToArray(), points.Select(p => p.Y).ToArray(), out a, out b, out c, out d);

        return $"y = {a}x^3 + {b}x^2 + {c}x + {d}";
    }

    private double CalculateSlope(double[] x, double[] y)
    {
        if (x.Length == 0 || y.Length == 0)
        {
            throw new InvalidOperationException("Cannot be empty");
        }

        double numerator = 0;
        double denominator = 0;
        double xAvg = x.Average();
        double yAvg = y.Average();
        for (int i = 0; i < x.Length; i++)
        {
            numerator = numerator + (x[i] - xAvg) * (y[i] - yAvg);
            denominator = denominator + Math.Pow(x[i] - xAvg, 2);
        }

        double slope = numerator / denominator;

        return slope;
    }

    private double CalculateIntercept(double[] x, double[] y)
    {
        if (x.Length == 0 || y.Length == 0)
        {
            throw new InvalidOperationException("Cannot be empty");
        }

        double numerator = 0;
        double denominator = 0;
        double xAvg = x.Average();
        double yAvg = y.Average();
        for (int i = 0; i < x.Length; i++)
        {
            numerator = numerator + (x[i] - xAvg) * (y[i] - yAvg);
            denominator = denominator + Math.Pow(x[i] - xAvg, 2);
        }

        double slope = CalculateSlope(x, y);
        double intercept = yAvg - slope * xAvg;

        return intercept;
    }


    private List<PointModel> CalculateBestFit(List<PointModel> points, string curveType)
    {
        if (curveType == "linear")
        {
            double m = 0;
            double b = 0;
            LinearRegression(points.Select(p => p.X).ToArray(), points.Select(p => p.Y).ToArray(), out m, out b);

            List<PointModel> bestFitLine = new List<PointModel>();
            foreach (var point in points)
            {
                double y = m * point.X + b;
                bestFitLine.Add(new PointModel { X = point.X, Y = y });
            }

            return bestFitLine;
        }
        else if (curveType == "quadratic")
        {
            double a, b, c;
            QuadraticRegression(points.Select(p => p.X).ToArray(), points.Select(p => p.Y).ToArray(), out a, out b, out c);

            List<PointModel> bestFitQuadraticCurve = new List<PointModel>();
            foreach (var point in points)
            {
                double y = a * Math.Pow(point.X, 2) + b * point.X + c;
                bestFitQuadraticCurve.Add(new PointModel { X = point.X, Y = y });
            }
            return bestFitQuadraticCurve;
        }

        else if (curveType == "cubic")
        {
            double a, b, c, d;
            CubicRegression(points.Select(p => p.X).ToArray(), points.Select(p => p.Y).ToArray(), out a, out b, out c, out d);

            List<PointModel> bestFitCubicCurve = new List<PointModel>();
            foreach (var point in points)
            {
                double y = a * Math.Pow(point.X, 3) + b * Math.Pow(point.X, 2) + c * point.X + d;
                bestFitCubicCurve.Add(new PointModel { X = point.X, Y = y });
            }
            return bestFitCubicCurve;
        }
        return new List<PointModel>();
    }

    private void LinearRegression(double[] x, double[] y, out double slope, out double intercept)
    {
        if (x.Length == 0 || y.Length == 0)
        {
            throw new InvalidOperationException("Arrays x and y must not be empty.");
        }

        double numerator = 0;
        double denominator = 0;
        double xAvg = x.Average();
        double yAvg = y.Average();
        for (int i = 0; i < x.Length; i++)
        {
            numerator = numerator + (x[i] - xAvg) * (y[i] - yAvg);
            denominator = denominator + Math.Pow(x[i] - xAvg, 2);
        }

        slope = numerator / denominator;
        intercept = yAvg - slope * xAvg;
    }

    private void QuadraticRegression(double[] x, double[] y, out double a, out double b, out double c)
    {
        var coefficients = Fit.Polynomial(x, y, 2);
        a = coefficients[2];
        b = coefficients[1];
        c = coefficients[0];
    }

    private void CubicRegression(double[] x, double[] y, out double a, out double b, out double c, out double d)
    {
        var coefficients = Fit.Polynomial(x, y, 3);
        a = coefficients[3];
        b = coefficients[2];
        c = coefficients[1];
        d = coefficients[0];
    }

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }
}
