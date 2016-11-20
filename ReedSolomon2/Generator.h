#pragma once
#include <cstdint>

namespace ReedSolomon {

	class Generator {

	public:

		Generator(int nParityCodewords);
		~Generator();

		inline size_t GetNParityCodewords() const { return _nParityCodewords; }
		inline uint16_t* GetCoefficients() const { return _coefficients; }

	private:

		size_t _nParityCodewords;
		uint16_t* _coefficients;
	};
}
