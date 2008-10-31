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

namespace QLNet {
    public class FixedRateBond : Bond {
        protected Frequency frequency_;
        protected DayCounter dayCounter_;

                      //BusinessDayConvention paymentConvention = Following,
                      //Real redemption = 100.0,
                      //const Date& issueDate = Date());
        public FixedRateBond(int settlementDays, double faceAmount, Schedule schedule, List<double> coupons,
                             DayCounter accrualDayCounter)
            : this(settlementDays, faceAmount, schedule, coupons, accrualDayCounter, BusinessDayConvention.Following, 100, null) { }
        public FixedRateBond(int settlementDays, double faceAmount, Schedule schedule, List<double> coupons,
                             DayCounter accrualDayCounter, BusinessDayConvention paymentConvention,
                             double redemption, Date issueDate)
                : base(settlementDays, schedule.calendar(), issueDate) {
            frequency_ = schedule.tenor().frequency();
            dayCounter_ = accrualDayCounter;
            maturityDate_ = schedule.endDate();

            cashflows_ = new FixedRateLeg(schedule, accrualDayCounter)
                                         .withNotionals(faceAmount)
                                         .withCouponRates(coupons)
                                         .withPaymentAdjustment(paymentConvention);

            addRedemptionsToCashflows(new List<double>() { redemption });

            if (cashflows().Count == 0)
                throw new ApplicationException("bond with no cashflows!");
            if (redemptions_.Count != 1)
                throw new ApplicationException("multiple redemptions created");
        }

        public FixedRateBond(int settlementDays, Calendar calendar,
                             double faceAmount,
                             Date startDate,
                             Date maturityDate,
                             Period tenor,
                             List<double> coupons,
                             DayCounter accrualDayCounter,
                             BusinessDayConvention accrualConvention,
                             BusinessDayConvention paymentConvention,
                             double redemption,
                             Date issueDate,
                             Date stubDate,
                             DateGeneration.Rule rule,
                             bool endOfMonth)
            : base(settlementDays, calendar, issueDate) {

            frequency_ = tenor.frequency();
            dayCounter_ = accrualDayCounter;

            maturityDate_     = maturityDate;

            Date firstDate, nextToLastDate;
            switch (rule) {
              case DateGeneration.Rule.Backward:
                firstDate = null;
                nextToLastDate = stubDate;
                break;
              case DateGeneration.Rule.Forward:
                firstDate = stubDate;
                nextToLastDate = null;
                break;
              case DateGeneration.Rule.Zero:
              case DateGeneration.Rule.ThirdWednesday:
              case DateGeneration.Rule.Twentieth:
              case DateGeneration.Rule.TwentiethIMM:
                    throw new ApplicationException("stub date (" + stubDate + ") not allowed with " + rule + " DateGeneration::Rule");
              default:
                    throw new ApplicationException("unknown DateGeneration::Rule (" + rule + ")");
            }

            Schedule schedule = new Schedule(startDate, maturityDate_, tenor,
                                             calendar_, accrualConvention, accrualConvention,
                                             rule, endOfMonth,
                                             firstDate, nextToLastDate);

            cashflows_ = new FixedRateLeg(schedule, accrualDayCounter)
                            .withNotionals(faceAmount)
                            .withCouponRates(coupons)
                            .withPaymentAdjustment(paymentConvention)
                            .value();

            addRedemptionsToCashflows(new List<double>() { redemption });

            if (cashflows().Count == 0)
                throw new ApplicationException("bond with no cashflows!");
            if (redemptions_.Count != 1)
                throw new ApplicationException("multiple redemptions created");
        }
    }
}
