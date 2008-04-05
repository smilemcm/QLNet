/*
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
  
 This file is part of QLNet Project http://trac2.assembla.com/QLNet

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

namespace QLNet {
    public class DiscountingSwapEngine : Swap.Engine {
        private Handle<YieldTermStructure> discountCurve_;

        public DiscountingSwapEngine(Handle<YieldTermStructure> discountCurve) {
            discountCurve_ = discountCurve;

            if (!discountCurve_.empty())  // add to observers of discountCurve
                discountCurve_.registerWith(update);
        }

        // Instrument interface
        public override void calculate() {
            if (discountCurve_.empty()) throw new ArgumentException("no discounting term structure set");

            results_.value = 0;
            results_.errorEstimate = null;
            results_.legNPV = new Array<double>(arguments_.legs.Count);
            results_.legBPS = new Array<double>(arguments_.legs.Count);
            for (int i=0; i<arguments_.legs.Count; ++i) {
                results_.legNPV[i] = arguments_.payer[i] * CashFlows.npv(arguments_.legs[i], discountCurve_);
                results_.legBPS[i] = arguments_.payer[i] * CashFlows.bps(arguments_.legs[i], discountCurve_);
                results_.value += results_.legNPV[i];
            }
        }
    }
}
