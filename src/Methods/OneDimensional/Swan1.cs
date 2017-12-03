﻿using System;

using MathOptimizer.Parser.Func;
using MathOptimizer.Methods.Params;

namespace MathOptimizer.Methods.OneDimensional
{
    class Swan1 : OneDimensionalMethod
    {
        public override void run(Function f, ref Parameters parameters)
        {
            /* Get input parameters */
            double x = parameters.inParameters.StartPoint;
            double stepValue = parameters.inParameters.StepValue;

            /* Optimization */
            double step;

            // Set step
            step = stepValue;
            if (x != 0)
            {
                step = stepValue * Math.Abs(x);
            }

            // Set direction
            if (f.Evaluate(x + step) > f.Evaluate(x))
            {
                step = -step;
            }

            double fPrev;
            double fCur = f.Evaluate(x);
            int counter = 0;

            do
            {
                x += step;
                step *= 2;

                fPrev = fCur;
                fCur = f.Evaluate(x);

                counter++;
            }
            while (fCur <= fPrev);

            /* Set output parameters */
            double leftBorder = x;
            double rightBorder = x - step/2.0 - step/4.0;
            Utills.Normalization(ref leftBorder, ref rightBorder);
            Interval resultInterval = new Interval(leftBorder, rightBorder);

            parameters.outParameters.ResultInterval = resultInterval;
            parameters.outParameters.ResultPoint = resultInterval.Center;
            parameters.outParameters.Iterations = counter;
        }
    }
}
