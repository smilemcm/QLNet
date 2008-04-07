/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://www.qlnet.org

 QLNet is free software: you can redistribute it and/or modify it
 under the terms of the QLNet license.  You should have received a
 copy of the license along with this program; if not, license is  
 available online at <http://trac2.assembla.com/QLNet/wiki/License>.
  
 QLNet is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.
 
 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QLNet
{
    // Abstract instrument class. It defines the interface of concrete instruments
    public abstract class Instrument : LazyObject
    {
        // The value of these attributes and any other that derived classes might declare must be set during calculation.
        protected double? NPV_, errorEstimate_;
        protected Dictionary<string, double> additionalResults_;
        protected PricingEngine engine_;


        //! sets the pricing engine to be used.
        /*! calling this method will have no effects in case the performCalculation method
            was overridden in a derived class. */
        public void setPricingEngine(PricingEngine e)
        {
            if (engine_ != null) engine_.unregisterWith(update);
            engine_ = e;
            if (engine_ != null) engine_.registerWith(update);

            update();       // trigger (lazy) recalculation and notify observers
        }


        /*! When a derived argument structure is defined for an instrument,
         * this method should be overridden to fill it.
         * This is mandatory in case a pricing engine is used. */
        public virtual void setupArguments(PricingEngine.Arguments a) { throw new NotImplementedException(); }


        #region Lazy object interface
        protected override void calculate()
        {
            if (isExpired())
            {
                setupExpired();
                calculated_ = true;
            }
            else
            {
                base.calculate();
            }
        }

        /* In case a pricing engine is not used, this method must be overridden to perform the actual
           calculations and set any needed results.
         * In case a pricing engine is used, the default implementation can be used. */
        protected override void performCalculations()
        {
            if (engine_ == null) throw new ArgumentException("null pricing engine");
            engine_.Reset();
            setupArguments(engine_.getArguments());
            engine_.getArguments().Validate();
            engine_.calculate();
            fetchResults(engine_.getResults());
        }
        #endregion

        #region Results
        /*! When a derived result structure is defined for an instrument,
         * this method should be overridden to read from it.
         * This is mandatory in case a pricing engine is used.  */
        public virtual void fetchResults(PricingEngine.Results r)
        {
            Instrument.Results results = r as Instrument.Results;
            if (results == null) throw new ArgumentException("no results returned from pricing engine");
            NPV_ = results.value;
            errorEstimate_ = results.errorEstimate;
            additionalResults_ = results.additionalResults;
        }

        public double NPV()
        {              //! returns the net present value of the instrument.
            calculate();
            if (NPV_ == null) throw new ArgumentException("NPV not provided");
            return NPV_.GetValueOrDefault();
        }

        public double errorEstimate()
        {    //! returns the error estimate on the NPV when available.
            calculate();
            if (errorEstimate_ == null) throw new ArgumentException("error estimate not provided");
            return (double)errorEstimate_;
        }

        // returns any additional result returned by the pricing engine.
        public double result(string tag)
        {
            calculate();
            try
            {
                return additionalResults_[tag];
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException(tag + " not provided");
            }
        }

        // returns all additional result returned by the pricing engine.
        public Dictionary<string, double> additionalResults() { return additionalResults_; }
        #endregion

        // This method must leave the instrument in a consistent state when the expiration condition is met.
        protected virtual void setupExpired()
        {
            NPV_ = errorEstimate_ = null;
            additionalResults_.Clear();
        }

        public abstract bool isExpired();   //! returns whether the instrument is still tradable.


        public class Results : PricingEngine.Results
        {
            public double? value;
            public double? errorEstimate;
            public Dictionary<string, double> additionalResults = new Dictionary<string, double>();

            public override void Reset()
            {
                value = errorEstimate = null;
                additionalResults.Clear();
            }
        }
    }
}
