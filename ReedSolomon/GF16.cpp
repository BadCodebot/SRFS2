#include "pch.h"
#include "GF16.h"
#include <stdexcept>

namespace ReedSolomon {

	GF16 GF16::_instance = GF16();

	GF16::GF16() {

		// The starting value for log table construnction.  We shift this left with each subsequent entry, using the primitive polynomial
		// to handle an overflow.
		uint16_t value = 0x01u;
		for (int exponent = 0; exponent < MASK; exponent++) {
			expTable[exponent] = value;
			logTable[value] = exponent;
			// Check for overflow on the next shift left
			if ((value & HIGH_BIT) != 0) value = ((value << 1) & MASK) ^ PRIMITIVE_POLYNOMIAL;
			else value = value << 1;
		}
		// Add in the last value.
		expTable[MASK] = expTable[0];
		// The log of zero is undefined, but I do not want unitialized data.
		logTable[0] = 0;
	}

	GF16::~GF16() { }

	uint16_t GF16::Multiply(uint16_t x, uint16_t y) {
		if ((x == 0) || (y == 0)) return 0;

		int sum = _instance.logTable[x] + _instance.logTable[y];
		sum = (sum >> BITS) + (sum & MASK);
		return _instance.expTable[sum];
	}

	uint16_t GF16::Power(uint16_t x, int a) {
		if (x == 0) {
			if (a == 0) throw std::invalid_argument("0 raised to the 0 power is undefined.");
			return 0;
		}
		if (a == 0) return 1;
		return _instance.expTable[(_instance.logTable[x] * a) % MAX_VALUE];
	}

	uint16_t GF16::Inverse(uint16_t x) {
		if (x == 0) throw std::invalid_argument("Cannot take the inverse of zero");
		return _instance.expTable[MAX_VALUE - _instance.logTable[x]];
	}
}