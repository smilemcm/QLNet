﻿/*
 Copyright (C) 2009 Philippe Real (ph_real@hotmail.com)
 Copyright (C) 2008, 2009 , 2010  Andrea Maggiulli (a.maggiulli@gmail.com)
  
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
using System.Linq;

namespace QLNet
{
	/// <summary>
	/// settlement information
	/// </summary>
	public struct Settlement
	{
		public enum Type { Physical, Cash };
	}

	//! %Swaption class
	/*! \ingroup instruments

		\test
		- the correctness of the returned value is tested by checking
		  that the price of a payer (resp. receiver) swaption
		  decreases (resp. increases) with the strike.
		- the correctness of the returned value is tested by checking
		  that the price of a payer (resp. receiver) swaption
		  increases (resp. decreases) with the spread.
		- the correctness of the returned value is tested by checking
		  it against that of a swaption on a swap with no spread and a
		  correspondingly adjusted fixed rate.
		- the correctness of the returned value is tested by checking
		  it against a known good value.
		- the correctness of the returned value of cash settled swaptions
		  is tested by checking the modified annuity against a value
		  calculated without using the Swaption class.


		\todo add greeks and explicit exercise lag
	*/

	public class Swaption : Option
	{
		public Arguments arguments;
		public SwaptionEngine engine;

		private readonly VanillaSwap swap_;
		private readonly Settlement.Type settlementType_;

		public Swaption(VanillaSwap swap, Exercise exercise)
			: base(new Payoff(), exercise)
		{
			settlementType_ = Settlement.Type.Physical;
			swap_ = swap;
			swap_.registerWith(update);
		}

		public Swaption(VanillaSwap swap, Exercise exercise, Settlement.Type delivery)
			: base(new Payoff(), exercise)
		{
			settlementType_ = delivery;
			swap_ = swap;
			swap_.registerWith(update);
		}

		public override bool isExpired()
		{
			return exercise_.dates().Last() < Settings.evaluationDate();
		}

		public override void setupArguments(IPricingEngineArguments args)
		{
			swap_.setupArguments(args);

			Swaption.Arguments arguments = args as Swaption.Arguments;
			if (arguments == null)
				throw new ArgumentException("wrong argument type");
			arguments.swap = swap_;
			arguments.settlementType = settlementType_;
			arguments.exercise = exercise_;
		}

		void validate()
		{
			arguments.validate();
			if (arguments.swap == null)
				throw new ArgumentException("vanilla swap not set");
			if (arguments.exercise == null)
				throw new ArgumentException("exercise not set");
		}

		//! \name Inspectors
		//@{
		public Settlement.Type settlementType()
		{
			return settlementType_;
		}

		public VanillaSwap.Type type()
		{
			return swap_.swapType;
		}

		public VanillaSwap underlyingSwap()
		{
			return swap_;
		}

		//! implied volatility
		public double impliedVolatility(double targetValue, Handle<YieldTermStructure> discountCurve, double guess,
										double accuracy = 1.0e-4,
										int maxEvaluations = 100,
										double minVol = 1.0e-7,
										double maxVol = 4.0)
		{
			calculate();
			if (isExpired())
			{
				throw new ArgumentException("instrument expired");
			}
			SwaptionImpliedVolatilityHelper f = new SwaptionImpliedVolatilityHelper(this, discountCurve, targetValue);
			//Brent solver;
			NewtonSafe solver = new NewtonSafe();
			solver.setMaxEvaluations(maxEvaluations);
			return solver.solve(f, accuracy, guess, minVol, maxVol);
		}

		// arguments, pricing engine
		public new class Arguments : VanillaSwap.Arguments
		{
			public Exercise exercise;
			public VanillaSwap swap;
			public Settlement.Type settlementType;
			public Arguments()
			{
				settlementType = Settlement.Type.Physical;
			}

			public override void validate()
			{
				base.validate();
			}
		}

		private class SwaptionImpliedVolatilityHelper : ISolver1d
		{
			private readonly IPricingEngine engine_;
			private readonly Handle<YieldTermStructure> discountCurve_;
			private readonly double targetValue_;
			private readonly SimpleQuote vol_;
			private readonly Instrument.Results results_;

			public SwaptionImpliedVolatilityHelper(Swaption swaption, Handle<YieldTermStructure> discountCurve, double targetValue)
			{
				discountCurve_ = discountCurve;
				targetValue_ = targetValue;
				
				// set an implausible value, so that calculation is forced
				// at first ImpliedVolHelper::operator()(Volatility x) call
				vol_ = new SimpleQuote(-1.0);
				Handle<Quote> h = new Handle<Quote>(vol_);
				engine_ = new BlackSwaptionEngine(discountCurve_, h);
				swaption.setupArguments(engine_.getArguments());
				results_ = engine_.getResults() as Results;
			}

			public override double value(double x)
			{
				if (x != vol_.value())
				{
					vol_.setValue(x);
					engine_.calculate();
				}

				return results_.value.Value - targetValue_;
			}

			public override double derivative(double x)
			{
				if (x != vol_.value())
				{
					vol_.setValue(x);
					engine_.calculate();
				}

				if (!results_.additionalResults.Keys.Contains("vega"))
				{
					throw new Exception("vega not provided");
				}

				return (double)results_.additionalResults["vega"];
			}
		}
	}

	//! base class for swaption engines
	public abstract class SwaptionEngine : GenericEngine<Swaption.Arguments, Swaption.Results> { }

	
}



