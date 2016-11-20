#pragma once
#include <cstdint>
#include <tmmintrin.h>

namespace ReedSolomon {

	class GF16MultiplicationTable {

	public:

		GF16MultiplicationTable();
		~GF16MultiplicationTable();

		void MultiplyAndXor(uint16_t* source, __m128i* dest, int count);
		void Set(const __m128i& x);

	private:

		static bool staticInitialize();

		const static uint16_t PRIMITIVE_POLYNOMIAL = 0x100B;
		static int lookupIndices[256];
		__declspec(align(16)) static __m128i polynomial;
		static bool initialized;

		__m128i* low;
		__m128i* high;

	};
}

