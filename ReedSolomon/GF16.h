#pragma once
#include <cstdint>
#include <tmmintrin.h>

namespace ReedSolomon
{
	class TTables;

	class GF16 {

	public:

		static const int ELEMENT_COUNT = 0x10000;
		static const uint16_t MAX_VALUE = 0xFFFF;

		static uint16_t Multiply(uint16_t x, uint16_t y);

		static uint16_t Inverse(uint16_t x);

		static uint16_t Power(uint16_t x, int a);

		inline static uint16_t Add(uint16_t x, uint16_t y) { return x ^ y; }

		inline static uint16_t Exp(int x) { return _instance.expTable[x]; }

		inline static int Log(uint16_t x) { return _instance.logTable[x]; }

	private:

		GF16();
		~GF16();

		static const uint16_t PRIMITIVE_POLYNOMIAL = 0x100B;

		// The maximum integer value for this Galois field.
		// Used to mask integers to bring them within range after arithmetic operations.
		static const uint32_t MASK = 0xFFFF;

		// The highest bit for this Galois field.  
		// Used to determine when we will exceed the maximum integer size with the next shift left.
		static const uint32_t HIGH_BIT = 0x8000;
		static const int32_t BITS = 16;

		static GF16 _instance;

		int logTable[ELEMENT_COUNT];
		uint16_t expTable[ELEMENT_COUNT];
	};
}

